using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


#region Game Execution
public class Program
{
    static void Main(string[] args)
    {

        GameData game = GameData.Instance = new GameData();
        AI ai = new AI_V2();
        GameExecution.InitGameAndZoneInfo(game, DateTime.Now.Ticks);

        while (true)
        {
            GameExecution.InitTurnInfoAndCallStartUpdate(game, DateTime.Now.Ticks);
        }
    }
}

public class GameExecution
{
    public delegate void ProcessMethode();
    public static ProcessMethode Awake;
    public static ProcessMethode Start;
    public static ProcessMethode BeforeUpdate;
    public static ProcessMethode Update;
    public static ProcessMethode AfterUpdate;
    public static ProcessMethode EndTurn;


    public static void InitGameAndZoneInfo(GameData game, long now)
    {
        Timer.StartTime = now;
        if (Awake != null)
            Awake();
        string[] inputs;
        inputs = Console.ReadLine().Split(' ');
        game.playerCount = int.Parse(inputs[0]);
        GameData.sMyId = game.myId = int.Parse(inputs[1]);
        GameData.sEnemyId = game.enemyId = game.myId == 0 ? 1 : 0;
        game.zoneCount = int.Parse(inputs[2]);
        game.linkCount = int.Parse(inputs[3]);
        for (int i = 0; i < game.zoneCount; i++)
        {
            Zone newZone = new Zone();
            inputs = Console.ReadLine().Split(' ');
            newZone.id = int.Parse(inputs[0]);
            newZone.income = int.Parse(inputs[1]);
            Zone.Add(newZone.id, newZone);
        }
        for (int i = 0; i < game.linkCount; i++)
        {
            inputs = Console.ReadLine().Split(' ');
            Zone zone1 = Zone.Get(int.Parse(inputs[0]));
            Zone zone2 = Zone.Get(int.Parse(inputs[1]));
            zone1.AddLink(zone2);
            zone2.AddLink(zone1);
        }



    }

    public static void InitTurnInfoAndCallStartUpdate(GameData game, long now)
    {
        try { 
            Timer.StartTimeTurn = now;
            string[] inputs;
            int iFrame = game.frame;
            if (iFrame == 0)
            {
                if (Start != null)
                    Start();
            }
            if (BeforeUpdate != null)
                BeforeUpdate();
            game.myPlatinium = int.Parse(Console.ReadLine());
            for (int i = 0; i < Zone.Count; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                Zone zone = Zone.Get(int.Parse(inputs[0]));
                zone.SetZoneStateInfo(int.Parse(inputs[1]), int.Parse(inputs[2]), int.Parse(inputs[3]), int.Parse(inputs[4]), int.Parse(inputs[5]));
                if (iFrame == 0 && zone.data.ownerId != -1)
                {
                    game.SetPlayerOrigineZone(zone.data.ownerId == GameData.sEnemyId, zone);
                }
            }
            if (Update != null)
                Update();
            if (AfterUpdate != null)
                AfterUpdate();
        }
        catch (Exception e) { Debug.DebugLog("Exception occured:" + e); }
        if (EndTurn != null)
            EndTurn();
        game.frame++;
    }

}
#endregion
#region Game Data
public class GameData
{
    public static GameData Instance;
    public int playerCount = 2;
    public int myId = 0;
    public int enemyId = 1;
    public static int sMyId = 0;
    public static int sEnemyId = 1;

    public int neutralId = -1;
    public int zoneCount;
    public int linkCount;
    public int frame;
    public int myPlatinium;


    public Dictionary<int, ShortestWayNode> enemyFlagPath;
    public Dictionary<int, ShortestWayNode> myFlagPath;
    public Zone myFlagLocalisation;
    public Zone enemyFlagLocalisation;
    public int enemyDistance;
    public float dominancePourcent = 0.75f;
    //public FixedMap map;

    public void SetPlayerOrigineZone(bool isEnemy, Zone zone)
    {
        if (isEnemy)
        {
            enemyFlagLocalisation = zone;
            enemyFlagPath = AllWaysGoToRome.GetMap(zone);
        }
        else
        {
            myFlagLocalisation = zone;
            myFlagPath = AllWaysGoToRome.GetMap(zone);
        }
        if (myFlagLocalisation != null && enemyFlagPath != null)
        {
            ShortestWayNode swn = null;
            enemyFlagPath.TryGetValue(myFlagLocalisation.id, out swn);
            CostNodeZone nz = swn.GetNextNodeZone();
            enemyDistance = nz.cost;
        }
    }

}

public class Timer
{
    public static long StartTime;
    public static long StartTimeTurn;
    public static long TurnMaxTime = 100000000;
    public static long Time { get { return DateTime.Now.Ticks; } }
    public static long TimeSinceStart { get { return Time - StartTime; } }
    public static long TimeSinceStartTurn { get { return Time - StartTimeTurn; } }

}


#endregion
#region Zone
public class Zone
{
    public int id;
    public int income = 0;
    public ZoneData data = new ZoneData();

    public List<Zone> linkedZone = new List<Zone>();

    public delegate void OnZoneConquer(int oldOwner, int newOWner);
    public OnZoneConquer onNewOwnerDetected;

    public delegate void OnArmyMove(int playerIndex, Zone zone, int oldDroneNumber, int newDroneNumber);
    public OnArmyMove onArmyMove;
    public delegate void OnStationDiscovered(Zone zone);
    public OnStationDiscovered onStationDiscovered;
    public delegate void OnZoneBecomeVisiable(Zone zone);
    public OnZoneBecomeVisiable onZoneBecomeVisiable;


    /// <summary>
    /// Plain = 6 link -- na
    /// DeadEnd = 1 link --
    /// Path = 2 link -- not connected
    /// Edge = 2 link -- connected
    /// OneSquareBorder = 5 link 
    /// TwoSquareBorder = 4 other in a linkedlist
    /// Border = 3 other in a linkedlist
    /// </summary>
    public enum ZoneType { Unknown, Plain, DeadEnd, Path, PathStart, Edge, /*OneSquareBorder, TwoSquareBorder,*/ Border, Junction }
    public ZoneType zoneType = ZoneType.Unknown;

    public void AddLink(Zone zone)
    {

        linkedZone.Add(zone);
    }


    public void SetZoneStateInfo(int newOwner, int dronePlayer0, int dronePlayer1, int visible, int platinum)
    {
        if (GameData.sMyId == 1)
        {
            int tmp = dronePlayer1;
            dronePlayer1 = dronePlayer0;
            dronePlayer0 = tmp;
        }

        int oldOwner = data.ownerId;
        //Check and warn listener
        if (onNewOwnerDetected != null && oldOwner != newOwner)
        {
            onNewOwnerDetected(oldOwner, newOwner);
        }
        if (onArmyMove != null)
        {
            if (data.myPods != dronePlayer0)
                onArmyMove(GameData.sMyId, this, data.myPods, dronePlayer0);
        }
        if (onArmyMove != null)
        {
            if (data.enemyPods != dronePlayer1)
                onArmyMove(GameData.sEnemyId, this, data.enemyPods, dronePlayer1);
        }
        //APly new Data

        if (!data.visible && visible == 1)
        {
            if (onZoneBecomeVisiable != null)
            {
                onZoneBecomeVisiable(this);

            }
        }
        if (!data.visited && platinum > 0)
        {
            if (onStationDiscovered != null)
            {
                onStationDiscovered(this);

            }
        }

        data.ownerId = newOwner;
        data.myPods = dronePlayer0;
        data.enemyPods = dronePlayer1;
        data.visible = visible == 1;
        if (data.visible)
        {
            data.lastVisibleOwnerId = newOwner;
            data.viewed = true;
        }
        if (platinum > 0) data.platinum = platinum;

        //should be set by listening delegate event
        if (data.myPods > 0) { data.myMark++; data.visited = true; }
        if (data.enemyPods > 0) data.enemyMark++;
    }







    public static int Count { get { return zones.Count; } }
    public static Dictionary<int, Zone> zones = new Dictionary<int, Zone>();
    public static void Add(int id, Zone zone)
    {
        zones.Add(id, zone);
    }
    public static Zone Get(int id)
    {
        Zone val;
        zones.TryGetValue(id, out val);
        return val;
    }


    public static Zone[] GetZones()
    {
        Zone[] array = new Zone[zones.Values.Count];
        zones.Values.CopyTo(array, 0);
        return array;
    }

    public int GetMyDrones()
    {
        return data.myPods;
    }
    public int GetEnemyDrone()
    {
        return data.enemyPods;
    }

    public Zone GetRandomLinkedZone()
    {
        int num = MapsAndProcess.NextRandom % linkedZone.Count;
        return linkedZone[num];
    }
    public Zone GetNextNeutralZone()
    {
        foreach (Zone z in linkedZone)
            if (z.data.lastVisibleOwnerId == -1)
                return z;
        return null;
    }
    public Zone GetNextEnemyZone(int myId)
    {
        foreach (Zone z in linkedZone)
            if (z.data.lastVisibleOwnerId != myId && z.data.lastVisibleOwnerId != -1)
                return z;
        return null;
    }

    internal Zone GetNextIncomeZone(int userId)
    {
        foreach (Zone z in linkedZone)
            if (z.income > 0 && z.data.lastVisibleOwnerId != userId)
                return z;
        return null;
    }
    public Zone GetLessMarkZone()
    {
        int mark = int.MaxValue;
        Zone zone = null;
        foreach (Zone z in linkedZone)
        {
            if (z.data.myMark < mark)
            {
                zone = z; mark = z.data.myMark;
            }
        }
        return zone;
    }

    public Zone GetFirstEnemyAround()
    {
        foreach (Zone z in linkedZone)
            if (z.GetEnemyDrone() > 0)
                return z;
        return null;
    }


    public bool IsLinkedTo(Zone zone)
    {
        return linkedZone.Contains(zone);
    }

    public Zone GetOtherLinkedZone(Zone idToAvoid)
    {
        Zone val = null;
        foreach (Zone z in linkedZone)
            if (z != idToAvoid)
            {
                if (val == null)
                    return val = z;
            }
        return val;

    }
    public Zone GetLinkedId(int idZone)
    {
        Zone z = null;
        foreach (Zone zone in linkedZone)
            if (zone.id == idZone)
                return zone;
        return z;
    }


    public bool IsOwnedByEnemy()
    {
        return !IsNeutralZone() && !IsOwnedByMe();
    }

    public bool IsOwnedByMe()
    {
        return data.lastVisibleOwnerId == GameData.sMyId;
    }
    public bool IsNeutralZone()
    {
        return data.lastVisibleOwnerId == -1;
    }
    //TOADDATHOME
    //public static List<Zone> GetNext_TEST(Zone from)
    //{
    //    Zone[] array = Zone.GetZones();
    //   Dictionary<int,ShortestWayNode> allWays = AllWaysGoToRome.GetMap(from);

    //    var res = from a in array
    //              where a.IsNeutralZone()
    //              orderby GetDistanceFrom(a.id, allWays), a.data.platinum descending
    //              select a;
    //    return res.ToList();
    //}


    public static List<Zone> GetAllNextNeutral(Zone from = null)
    {
        Zone[] array = Zone.GetZones();
        Dictionary<int, ShortestWayNode> allWays = AllWaysGoToRome.GetMap(from);

        var res = from a in array
                  where a.IsNeutralZone()
                  orderby GetDistanceFrom(a.id, allWays) ascending, a.data.platinum descending
                  // orderby a.data.platinum descending
                  //testing
                  select a;
        return res.ToList();
    }

    public static List<Zone> GetAllUndiscoveredZone(Zone from = null)
    {
        Zone[] array = Zone.GetZones();
        Dictionary<int, ShortestWayNode> allWays = AllWaysGoToRome.GetMap(from);

        var res = from a in array
                  where a.data.viewed == false
                  orderby GetDistanceFrom(a.id, allWays)
                  // orderby a.data.platinum descending
                  //testing
                  select a;
        return res.ToList();
    }
    public static List<Zone> GetAllNextStation(Zone from, int minPlatinum = 1)
    {
        Zone[] array = Zone.GetZones();
        Dictionary<int, ShortestWayNode> allWays = AllWaysGoToRome.GetMap(from);

        var res = from a in array
                  where a.data.platinum >= minPlatinum && (a.IsNeutralZone() || a.IsOwnedByEnemy())
                  orderby GetDistanceFrom(a.id, allWays), a.data.platinum descending
                  select a;
        return res.ToList();
    }
    public static List<Zone> GetAllBlocusZone()
    {
        Zone[] array = Zone.GetZones();
        var res = from a in array
                  where a.data.isBlockadePoint
                  orderby a.data.myPods
                  select a;
        return res.ToList();
    }
    public static List<Zone> GetAllTerritory(int playerId)
    {
        Zone[] array = Zone.GetZones();
        var res = from a in array
                  where a.data.lastVisibleOwnerId == playerId
                  ////TOADDATHOME orderby a.data.platinum GetDistanceFrom(a.id, allWays) descending
                  orderby a.data.platinum descending
                  select a;
        return res.ToList();
    }

    public enum NextType { NeutralZone, StationZone, LessMinionBlockade, Undiscovered }
    public static Zone GetNext(NextType nextType, Zone from)
    {
        List<Zone> l = null;
        switch (nextType)
        {
            case NextType.StationZone: l = GetAllNextStation(from); break;
            case NextType.NeutralZone: l = GetAllNextNeutral(from); break;
            case NextType.LessMinionBlockade: l = GetAllBlocusZone(); break;
            case NextType.Undiscovered: l = GetAllUndiscoveredZone(from); break;
        }
        if (l == null || l.Count <= 0) return null;
        return l[0];
    }
    public static float GetPourcentWinTerritory()
    {
        List<Zone> l = GetAllTerritory(GameData.sMyId);
        if (Count == 0 || l == null) return 0f;
        return ((float)l.Count) / ((float)Count);
    }

    public static float GetPourcentExplored()
    {
        List<Zone> l = GetAllUndiscoveredZone(GameData.Instance.myFlagLocalisation);
        if (Count == 0 || l == null) return 0f;
        return ((float)l.Count) / ((float)Count);
    }

    public static Zone GetRandom()
    {
        return Get(MapsAndProcess.NextRandom % Count);

    }

    public static int GetDistanceFrom(int zoneId, Dictionary<int, ShortestWayNode> allWaysToZone)
    {
        if (!allWaysToZone.ContainsKey(zoneId)) return int.MaxValue;
        CostNodeZone node = allWaysToZone[zoneId].GetNextNodeZone();
        if (node == null) return int.MaxValue;
        return node.cost;
    }





}
public class ZoneData
{
    public int ownerId = -1;
    public int lastVisibleOwnerId = -1;
    public int myPods;
    public int enemyPods;
    public int platinum;

    public bool visited;
    public bool viewed;
    public bool visible;


    public int stolenCount;

    public int enemyMark;
    public int myMark;
    public bool isBlockadePoint;

    public override string ToString()
    {
        return string.Format("Owner: {0} (last:{1}) my {2}, enemy {3}, plat {4} vis {5}", ownerId, lastVisibleOwnerId, myPods, enemyPods, platinum, visible);
    }

}
#endregion
#region Path management zone
public class AllWaysGoToRome
{
    public static Dictionary<int, ShortestWayNode> GetMap(Zone target)
    {
        if (target == null)
            return null;
        Dictionary<int, ShortestWayNode> map = new Dictionary<int, ShortestWayNode>();
        List<Zone> leaves = new List<Zone>();
        Dictionary<int, Zone> isOut = new Dictionary<int, Zone>();
        Explore(target, ref leaves, ref isOut, ref map);

        return map;
    }

    private static void Explore(Zone target, ref List<Zone> leaves, ref Dictionary<int, Zone> isOut, ref Dictionary<int, ShortestWayNode> map)
    {
        int turnCount = 0;
        //Element à traiter
        List<Zone> currentList = new List<Zone>();
        //les prochains qui seront traiter le tours suivant
        List<Zone> nextList = new List<Zone>();
        // les feuilles de l'élément traiter courant.
        List<Zone> leavesList = new List<Zone>();
        currentList.Add(target);

        while (currentList.Count > 0)
        {
            //Chaque tour, il y a un zone en plus à marcher
            turnCount++;

            //Les éléments en cours en doivent pas être réutiliser
            foreach (Zone current in currentList)
                isOut.Add(current.id, current);
            //Pour chaque élément récupérer les feuilles et créer un lien avec le nombre de ressources pour arrivés à destination
            foreach (Zone current in currentList)
            {
                leavesList.Clear();
                GetZoneLeaf(current, ref leavesList, ref nextList, ref isOut);
                foreach (Zone leaf in leavesList)
                {
                    AddLeafToMap(current, leaf, turnCount, ref map);
                }
            }

            ClearCurrentAndInverse(ref currentList, ref nextList);
        }

    }

    private static void ClearCurrentAndInverse(ref List<Zone> currentList, ref List<Zone> nextList)
    {
        currentList.Clear();
        List<Zone> tmp = currentList;
        currentList = nextList;
        nextList = tmp;
    }
    /// <summary>
    /// J'ajouter les feuilles d'exploration à la carte.
    /// L'idée est que chaque case doit possèder des liens vers c'est parent du tour avant.
    /// Et le nombre de ressource pour arriver au point voulu
    /// </summary>
    /// <param name="parent"> The direction where to go</param>
    /// <param name="leaf">La zone de feuille à ajoute à la map</param>
    /// <param name="currentCost">Le cout total pour arrivé à destination</param>
    /// <param name="map">La carte actuelle</param>
    private static void AddLeafToMap(Zone parent, Zone leaf, int currentCost, ref Dictionary<int, ShortestWayNode> map)
    {
        ShortestWayNode swn = null;
        map.TryGetValue(leaf.id, out swn);
        if (swn == null)
        {
            swn = new ShortestWayNode(leaf);
            map.Add(leaf.id, swn);
        }
        CostNodeZone nz = new CostNodeZone(parent, currentCost);
        swn.Add(nz);
    }

    /// <summary>
    /// Je récupère les feuilles de l'élément courant qui ne sont pas figer
    /// </summary>
    /// <param name="current">Branche</param>
    /// <param name="leaves">Feuilles à traiter si pas fixe</param>
    /// <param name="next">List qui contine toutes le feuille traiter ce tour pour les utilisers au prochain</param>
    /// <param name="isOut"> les éléments déjà traité  et donc fixe </param>
    private static void GetZoneLeaf(Zone current, ref List<Zone> leaves, ref List<Zone> next, ref Dictionary<int, Zone> isOut)
    {

        foreach (Zone linkToCurrent in current.linkedZone)
            if (!isOut.ContainsKey(linkToCurrent.id))
            {
                leaves.Add(linkToCurrent);
                if (!next.Contains(linkToCurrent))
                    next.Add(linkToCurrent);

            }
    }


    public static Queue<int> GetPath(Zone target, Zone from)
    {
        Queue<int> queue = new Queue<int>();
        Dictionary<int, ShortestWayNode> mapPath = GetMap(target);

        int nextId = from.id;
        CostNodeZone nextNode;
        int lenght = 0;
        try
        {
            do
            {
                nextNode = mapPath[nextId].GetNextNodeZone();
                lenght = nextNode.cost;
                nextId = nextNode.zone.id;
                queue.Enqueue(nextId);
                //   Debug.DebugLog("P: " + nextId+" L:"+lenght);
            } while (lenght > 1);

        }
        catch (Exception) { }
        return queue;
    }
}

public class ShortestWayNode
{
    public Zone zone;
    public List<CostNodeZone> directions = new List<CostNodeZone>();


    public ShortestWayNode(Zone zone)
    {
        this.zone = zone;
    }
    public void Add(CostNodeZone node)
    {
        if (node != null)
            directions.Add(node);
    }

    public Zone GetNextZone()
    {
        int minCost = 10000;
        Zone next = null;
        foreach (CostNodeZone nz in directions)
            if (nz.cost < minCost)
            {
                next = nz.zone;
                minCost = nz.cost;
            }
        return next;
    }
    public CostNodeZone GetNextNodeZone()
    {
        int minCost = 10000;
        CostNodeZone next = null;
        foreach (CostNodeZone nz in directions)
            if (nz.cost < minCost)
            {
                next = nz;
                minCost = nz.cost;
            }
        return next;
    }


}
public class CostNodeZone
{
    public Zone zone;
    public int cost;
    public CostNodeZone(Zone zone, int cost)
    {
        this.zone = zone;
        this.cost = cost;
    }
}
#endregion
#region Process Methode
class MyMath
{
    public static int Clamp(int value, int min, int max)
    {
        return (value < min) ? min : (value > max) ? max : value;
    }
}

public class MapsAndProcess
{
    public static Zone.ZoneType DefineZoneType(Zone zone, List<Zone> linkedZone)
    {
        if (linkedZone.Count == 6) return Zone.ZoneType.Plain;
        if (linkedZone.Count == 1) return Zone.ZoneType.DeadEnd;
        if (linkedZone.Count == 2)
        {
            if (linkedZone[0].IsLinkedTo(linkedZone[1]))
                return Zone.ZoneType.Path;
            else return Zone.ZoneType.Edge;
        }
        if (linkedZone.Count == 3)
        {
            Zone z1, z2, z3;
            z1 = linkedZone[0];
            z2 = linkedZone[1];
            z3 = linkedZone[2];
            if (z1.IsLinkedTo(z2) || z1.IsLinkedTo(z3) || z2.IsLinkedTo(z1) || z1.IsLinkedTo(z3) || z3.IsLinkedTo(z1) || z3.IsLinkedTo(z2))
                return Zone.ZoneType.Junction;
        }

        return Zone.ZoneType.Unknown;
    }


    private static int RandomCount;
    public static int NextRandom
    {
        get
        {
            RandomCount++;
            Random r = new Random(DateTime.Now.Millisecond + RandomCount);
            return r.Next();
        }
    }
}
#endregion

#region Game Command
public class GameCommand
{
    private static string moves = "";
    public static void MoveDrone(int droneNumber, int from, int to)
    {
        bool first = moves.Length <= 0;
        moves += String.Format("{0}{1} {2} {3}", first ? "" : " ", droneNumber, from, to);
    }
    private static string purchases = "";
    public static void BuyDrone(int droneNumber, int where)
    {
        bool first = purchases.Length <= 0;
        purchases += String.Format("{0}{1} {2}", first ? "" : " ", droneNumber, where);
    }

    public static void Reset() { purchases = ""; moves = ""; }
    public static void ExecuteOrder()
    {
        if (moves.Length <= 0)
            Console.WriteLine("WAIT");
        else Console.WriteLine(moves);

        if (purchases.Length <= 0)
            Console.WriteLine("WAIT");
        else Console.WriteLine(purchases);
    }
}
#endregion
#region Debug

public class Debug
{
    public static void DebugLog(string message) { Console.Error.WriteLine(message); }

    public static void DebugLogStateGame_StartUpdate()
    {
        GameData game = GameData.Instance;
        if (game == null) return;
        DebugLog(string.Format("Game state: (f:{0})  Time: {1}", game.frame, Timer.TimeSinceStart / 1000000000.0));
        DebugLog(string.Format("----------- Start-------------"));
        DebugLog(string.Format("ID: {0}  Map: {1} ({2}) ", GameData.sMyId, "ToImpl", game.zoneCount));
        DebugLog(string.Format("Start id, Me: {0}  Enemy : {1}", game.myFlagLocalisation.id, game.enemyFlagLocalisation.id));

    }
    public static void DebugLogBattleState()
    {

        DebugLog(string.Format("##  Battle State  ## "));
        DebugLog(string.Format("Pct conq. {0} %   Explored {1} %", (int)(Zone.GetPourcentWinTerritory() * 100f), (int)(Zone.GetPourcentExplored() * 100f)));
        DebugLog(string.Format("_____________Squads_________________"));
        //foreach (Squad s in Squad.GetAllSquad())
        //  DebugLog(s.ToString());
        Squad[] array = Squad.GetAllSquad();
        for (int i = 0; i < array.Length; i++)
        {
            //PRINT DEBUG
            // DebugLog(array[i].ToString());
        }



    }
    public static void DebugLogStateGame_EndUpdate()
    {
        DebugLog(string.Format("______________________________"));
        double pctUsed = (double)Timer.StartTimeTurn / (double)Timer.TurnMaxTime;
        pctUsed = MyMath.Clamp((int)(pctUsed * 100.0), 0, 100);

        DebugLog(string.Format("Time used: {0} ({1}%)", Timer.TimeSinceStartTurn / 1000000000.0, pctUsed));
        DebugLog(string.Format("-------------END--------------"));
    }


}
#endregion



#region Map detection

public struct FixedMap
{
    public string mapName;
    public int zoneNumber;
    public MapZone[] markedZone;

    public bool IsItThisMap(ref Dictionary<int, Zone> existingZone, out string name)
    {
        name = "";
        if (existingZone.Count - 1 != zoneNumber) return false;
        foreach (MapZone mz in markedZone)
            if (!mz.IsZoneBelongToMap(ref existingZone))
                return false;
        name = this.mapName;
        return true;
    }
    public override string ToString()
    {
        string description = string.Format("{0} ({1})", mapName, zoneNumber);
        foreach (MapZone zone in markedZone)
            description += " " + zone;
        return description;
    }
}
public struct MapZone
{
    public int zoneId;
    public int[] friendZoneId;

    public MapZone(int mainZoneId, int[] linkedZoneId)
    {
        // TODO: Complete member initialization
        this.zoneId = mainZoneId;
        this.friendZoneId = linkedZoneId;
    }
    public bool IsZoneBelongToMap(ref Dictionary<int, Zone> existingZone)
    {
        Zone center = null;
        if (!existingZone.ContainsKey(zoneId)) return false;
        existingZone.TryGetValue(zoneId, out center);
        if (center == null) return false;

        foreach (int izone in friendZoneId)
        {
            if (!existingZone.ContainsKey(izone)) return false;
            if (center.GetLinkedId(izone) == null) return false;
        }
        return true;
    }
    public override string ToString()
    {
        string description = "ID " + zoneId + " (";
        foreach (int id in friendZoneId)
            description += " " + id;
        return description + ")";
    }
}

public class MapArea
{
    public int[] zoneId;

    public MapArea(int[] zoneId) { this.zoneId = zoneId; }

    public bool Contains(int wantedId)
    {
        foreach (int id in zoneId)
            return true;
        return false;
    }

    //public float GetPourcentControledBy(int playerId)
    //{
    //    //TODO
    //    return 1f;
    //}
    public override string ToString()
    {
        string description = "Map Area: ";
        foreach (int id in zoneId)
            description += " " + id;
        return description;
    }
}

#endregion



#region Strategie to apply

public abstract class AI
{
    public AI()
    {
        GameExecution.Awake += Awake;
        GameExecution.Start += Start;
        GameExecution.BeforeUpdate += BeforeUpdate;
        GameExecution.Update += Update;
        GameExecution.AfterUpdate += AfterUpdate;
        GameExecution.EndTurn += EndTurn;
    }

    public abstract void Awake();
    public abstract void Start();
    public abstract void BeforeUpdate();
    public abstract void AfterUpdate();
    public abstract void Update();
    public abstract void EndTurn();

}
public class AI_V2 : AI
{
    public AI_V2() : base() { }
    public override void Awake()
    {
    }

    public override void Start()
    {
    }

    public override void BeforeUpdate()
    {
        GameCommand.Reset();
        GameCommand.BuyDrone((int)(GameData.Instance.myPlatinium / 20f), 1);

        //foreach(Zone z in Zone.GetZones())
        //if(z.data.visible)
        //     Debug.DebugLog("Z "+z.id+":"+z.data.ToString());
    }

    public override void AfterUpdate()
    {
        Debug.DebugLogStateGame_EndUpdate();
    }

    public override void Update()
    {
        Debug.DebugLogStateGame_StartUpdate();

        SquadStateRefresh();
        SquadFactory();
        SquadCheckForMissionChange();
        SquadMoveAll();

        Debug.DebugLogBattleState();
    }



    public override void EndTurn()
    {
        GameCommand.ExecuteOrder();
    }



    private void SquadStateRefresh()
    {
        int[] soldiers = GetAllMySolidersByZone();
        foreach (Squad s in Squad.GetAllSquad())
        {
            s.SetNewPosition(s.nextPosition);
            if (s.UpdgradeSoldierAllocState(ref soldiers[s.currentPosition.id]))
            {
            }
        }
    }
    private void SquadFactory()
    {
        GameData game = GameData.Instance;
        Zone startBase = game.myFlagLocalisation;
        int unityOnBase = startBase.GetMyDrones();
        bool dominance = Zone.GetPourcentWinTerritory() > game.dominancePourcent;

        if (game.enemyDistance < 9 && game.frame < game.enemyDistance + 2)
        {
            if (game.frame == 0)
            {
                int randomId = MapsAndProcess.NextRandom % Zone.GetZones().Length;
                Squad s = new Squad(1, startBase);
                s.SetBehavior(BehaviorStorage.ExplorerBehaviour);
                s.GoToZone(randomId);

                s = new Squad(5, startBase);
                //       FORTEST       
                s.SetBehavior(BehaviorStorage.GoCaptureFlagBehaviour);
                //s.SetBehavior(BehaviorStorage.ExplorerBehaviour);
            }

            return;
        }


        if (dominance)
        {
            if (unityOnBase >= 30)
            {
                Squad s = new Squad(15, startBase);
                s.SetBehavior(BehaviorStorage.GoCaptureFlagBehaviour);
                s.GoToZone(startBase.GetRandomLinkedZone().id);
            }
            return;
        }
        if (unityOnBase >= 6)
        {
            foreach (Zone z in startBase.linkedZone)
            {
                int randomId = MapsAndProcess.NextRandom % Zone.GetZones().Length;
                unityOnBase--;
                Squad s = new Squad(1, startBase);
                if (unityOnBase % 2 == 0)
                    s.SetBehavior(BehaviorStorage.ExplorerBehaviour);
                else
                    s.SetBehavior(BehaviorStorage.CaptureStationBehaviour);
                //s.GoToNextZone(z.id);
                s.GoToZone(randomId);
                // Debug.DebugLog("> Unity Created:" + s);
            }
            if (unityOnBase > 0)
            {
                Squad s = new Squad(unityOnBase, startBase);
                s.GoToNextZone(game.enemyFlagPath[startBase.id].GetNextZone().id);
                // FORTEST
                s.SetBehavior(BehaviorStorage.GoCaptureFlagBehaviour);
                //s.SetBehavior(BehaviorStorage.ExplorerBehaviour);
                // Debug.DebugLog("> Unity Created:" + s);
                unityOnBase = 0;
            }
        }
        else
        {
            foreach (Zone z in startBase.linkedZone)
            {
                int randomId = MapsAndProcess.NextRandom % Zone.GetZones().Length;

                unityOnBase--;
                Squad s = new Squad(1, startBase);

                if (unityOnBase % 2 == 0)
                    s.SetBehavior(BehaviorStorage.ExplorerBehaviour);
                else
                    s.SetBehavior(BehaviorStorage.CaptureStationBehaviour);

                s.GoToZone(randomId);
                // Debug.DebugLog("> Unity Created:" + s);
            }
        }



    }
    private void SquadMoveAll()
    {
        foreach (Squad s in Squad.GetAllSquad())
        {
            if (s.DoTheThing() != null)
            {
                GameCommand.MoveDrone(s.soldiers, s.currentPosition.id, s.nextPosition.id);
            }
        }
    }

    private void SquadCheckForMissionChange()
    {
        GameData game = GameData.Instance;
        bool dominance = Zone.GetPourcentWinTerritory() > game.dominancePourcent;
        int lengthToAttackFlag = 0; int frameLess = 235 - game.frame;

        foreach (Squad s in Squad.GetAllSquad())
        {
            lengthToAttackFlag = game.enemyFlagPath[s.currentPosition.id].GetNextNodeZone().cost;
            bool itIsTimeToEnd = lengthToAttackFlag == frameLess;
            if (dominance || itIsTimeToEnd)
                s.SetBehavior(BehaviorStorage.GoCaptureFlagBehaviour);
        }

    }





    private int[] GetAllMySolidersByZone()
    {
        int[] z = new int[Zone.Count];
        for (int i = 0; i < Zone.Count; i++)
        {
            z[i] = Zone.Get(i).data.myPods;
        }
        return z;
    }


}

public class Squad
{
    public int soldiers;
    public int intialSoldiers;

    public Zone nextPosition;
    public Zone currentPosition;
    public Zone lastPosition;
    public Queue<int> wantedPath = new Queue<int>();
    public delegate void Behaviour(Squad escade);
    private Behaviour behaviour;

    public Squad(int soldierCount, Zone currentPosition)
    {
        intialSoldiers = soldiers = soldierCount;
        this.currentPosition = currentPosition;
        Created();
    }
    // return is the squad death
    public bool UpdgradeSoldierAllocState(ref int soldierCount)
    {
        if (currentPosition.id == GameData.Instance.myFlagLocalisation.id)
        { Destroy(); return true; }

        if (soldierCount >= this.soldiers)
        {
            soldierCount -= this.soldiers;
            return false;
        }
        else if (soldierCount > 0)
        {
            this.soldiers = soldierCount;
            soldierCount = 0;
            return false;
        }
        else
        {

            this.soldiers = 0;
            soldierCount = 0;
            Destroy();
            return true;
        }
    }
    void Destroy()
    {
        behaviour = null;
        AllSquad.Remove(this);
    }
    void Created() { behaviour = defaultBehaviour; AllSquad.Add(this); }
    public void SetBehavior(Behaviour behaviour) { this.behaviour = behaviour; }

    public static Behaviour defaultBehaviour = BehaviorStorage.DefaultBehaviour;
    static List<Squad> AllSquad = new List<Squad>();
    public static Squad[] GetAllSquad()
    {
        //Squad [] array=  new Squad[AllSquad.Count];
        return AllSquad.ToArray();
    }

    public Zone DoTheThing()
    {
        if (behaviour != null)
        { behaviour(this); }
        return nextPosition;
    }
    public override string ToString()
    {
        return string.Format("Sq: {0} from {1} to {2}", soldiers, currentPosition.id, nextPosition != null ? nextPosition.id : -1);
    }

    public void SetNewPosition(Zone zone)
    {
        if (zone != null)
        {
            lastPosition = currentPosition;
            currentPosition = zone;
        }
        nextPosition = null;
    }

    public void GoToNextZone(int zoneId)
    {
        if (currentPosition.GetLinkedId(zoneId) != null)
        {
            wantedPath.Enqueue(zoneId);
        }
    }
    public void GoToZone(int zoneId)
    {
        Zone g = Zone.Get(zoneId);
        if (g == null) return;
        wantedPath = AllWaysGoToRome.GetPath(g, currentPosition);


    }
}

public class BehaviorStorage
{
    public static void DefaultBehaviour(Squad squad)
    {
        RandomBehavior(squad);
    }
    public static void DefensiveBehaviour(Squad squad)
    {

    }
    public static void ExplorerBehaviour(Squad squad)
    {

        if (PathDefineBehaviour(squad)) return;
        Zone z = squad.currentPosition;
        //Zone goTo = z.GetNextIncomeZone(GameData.sMyId);
        //if (goTo != null) { squad.nextPosition = goTo; return; }
        //goTo = z.GetNextNeutralZone();
        //if (goTo != null) { squad.nextPosition = goTo; return; }

        Zone nextZone = Zone.GetNext(Zone.NextType.NeutralZone, z);
        Debug.DebugLog("NEXT Zone " + squad.currentPosition.id + " to " + nextZone.id + "  " + nextZone.data.ownerId);
        if (nextZone != null)
        {
            squad.GoToZone(nextZone.id);
        }
        if (PathDefineBehaviour(squad)) return;
        squad.SetBehavior(CaptureStationBehaviour);
    }
    public static void CaptureStationBehaviour(Squad squad)
    {
        if (PathDefineBehaviour(squad)) return;
        Zone nextZone = Zone.GetNext(Zone.NextType.StationZone, squad.currentPosition);
        if (nextZone != null)
        {
            squad.GoToZone(nextZone.id);
        }
        if (PathDefineBehaviour(squad)) return;
        ExplorerBehaviour(squad);

    }
    public static void BlockadeBehaviour(Squad squad)
    {
        if (PathDefineBehaviour(squad)) return;
        Zone nextZone = Zone.GetNext(Zone.NextType.LessMinionBlockade, squad.currentPosition);
        if (nextZone != null)
        {
            squad.GoToZone(nextZone.id);
            return;
        }
    }
    public static bool PathDefineBehaviour(Squad squad)
    {
        Zone z = squad.currentPosition;
        if (squad.wantedPath.Count > 0)
        {
            squad.nextPosition = z.GetLinkedId(squad.wantedPath.Dequeue());
            return true;
        }
        return false;
    }

    public static void RandomBehavior(Squad squad)
    {
        if (PathDefineBehaviour(squad)) return;
        Zone z = squad.currentPosition;
        squad.nextPosition = z.GetRandomLinkedZone();
    }

    public static void GoCaptureFlagBehaviour(Squad squad)
    {
        if (PathDefineBehaviour(squad)) return;
        Zone z = squad.currentPosition;
        squad.nextPosition = GameData.Instance.enemyFlagPath[z.id].GetNextZone();
    }
}

#endregion