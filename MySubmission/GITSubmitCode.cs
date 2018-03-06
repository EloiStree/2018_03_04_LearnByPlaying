using System; using System.Linq; using System.IO; using System.Text; using System.Collections; using System.Collections.Generic; 
public class Arguments : Singleton<Arguments>
{
 public List<string> Lines = new List<string>();
 public string Add(string line) { Lines.Add(line); return line; }
 public int Add(int line) { Lines.Add(""+line ); return line; }
 public void Clear() { Lines.Clear(); }
}
public class ClaimMap : Singleton<ClaimMap>
{
 public Dictionary<int, int> availaibleUnity = new Dictionary<int, int>();
 public Dictionary<int, Claim> existingClaim = new Dictionary<int, Claim>();
 public Dictionary<int, AllowedClaim> allowedThisTurn = new Dictionary<int, AllowedClaim>();
 public List<Claim> AllClaims() { return existingClaim.Values.ToList(); }
 public List<Claim> TurnClaim(int turn)
 {
 return existingClaim.Values.Where(c => c.WhenToApply == turn).ToList();
 }
 public List<Claim> OutdatedClaim(int turn)
 {
 return existingClaim.Values.Where(c => c.WhenToApply < turn).ToList();
 }
 public List<Claim> TurnClaimByPriority(int turn)
 {
 return TurnClaim(turn).OrderByDescending(c => c.Priority).ToList();
 }
 public int UnitNotClaimed(Factory f)
 {
 List<Claim> claims = TurnClaim(R.Turn).Where(c => c.Where == f).ToList();
 return availaibleUnity[f.ID] - claims.Sum(c => c.RequiredUnit);
 }
 public void SetUnitsAvaialaible(List<Factory> factories) {
 foreach (Factory f in factories)
 {
 availaibleUnity[f.ID] = f.Units;
 }
 allowedThisTurn.Clear();
 foreach (Claim claim in OutdatedClaim(R.Turn)) {
 RemoveClaim(claim);
 }
 }
 public void SetAllowedClaim() {
 foreach (Claim cl in TurnClaimByPriority(R.Turn))
 {
 if (HasUnitsFor(cl)) {
 AddAllowClaim(cl); 
 }
 }
 }
 private void AddAllowClaim(Claim claim)
 {
 allowedThisTurn.Add(claim.ID, new AllowedClaim(claim.RequiredUnit,claim));
 }
 
 public bool HasUnitsFor(Claim claim)
 {
 return HasUnitsFor(claim.Where, claim.RequiredUnit);
 }
 public bool HasUnitsFor(Factory target, int numberRequired) {
 return UnitNotClaimed(target) >= numberRequired;
 }
 
 public void AddClaim(Claim claim) {
 if (existingClaim.ContainsKey(claim.ID))
 return;
 existingClaim.Add(claim.ID,claim);
 }
 public void CancelClaim(Claim claim)
 { RemoveClaim(claim);}
 public void RemoveClaim(Claim claim)
 {
 if (!existingClaim.ContainsKey(claim.ID))
 return;
 existingClaim.Remove(claim.ID);
 }
 internal bool IsAllow(Claim claim)
 {
 return allowedThisTurn.ContainsKey(claim.ID);
 }
}
public class AllowedClaim {
 public AllowedClaim(int requiredUnit, Claim claim)
 {
 SetAssignedUnits(requiredUnit);
 }
 public Claim Claim { get; private set; }
 public bool HasBeenAllowed { get; private set; }
 public int UnitsAssigned { get; private set; }
 public void SetAssignedUnits(int units)
 {
 HasBeenAllowed = units > 0;
 }
 public int RecoverUnity() {
 int units = UnitsAssigned;
 SetAssignedUnits(0);
 return units;
 }
}
public class Claim
{
 public static int IDCOUNT = 0;
 public string Description = "";
 public Claim(string description, Factory where,int when, int required, ClaimPriority priority=ClaimPriority.Low) :
 this(description, where, when ,required,required,priority) { }
 public Claim(string description, Factory where,int when, int minUnit, int required, ClaimPriority priority) {
 ID = IDCOUNT++;
 this.RequiredUnit = required;
 this.MinUnit = minUnit;
 this.Priority = priority;
 this.WhenToApply = when;
 this.Description = description;
 this.Where = where;
 }
 public int ID { get; private set; }
 int _requiredUnit;
 public int RequiredUnit { get { return UseAllUnit ?Where.Units : _requiredUnit; } private set { _requiredUnit = value; } }
 public int MinUnit { get; private set; }
 public ClaimPriority Priority { get; set; }
 public Factory Where { get; private set; }
 public int WhenToApply { get; private set; }
 public bool UseAllUnit = false;
 public bool HasBeenAllowed { get { return ClaimMap.I.IsAllow(this); } }
}
public enum ClaimPriority : int { Low=0, Medium=2, Normal=5, Hight=8, NotNegociable=100 }
 class Day3_SilverAI : TurnManagerAbstract
 {
 public override void BeforeExecuteTurn()
 {
 
 }
 public override void ExecuteTurn()
 {
 Debug.Log(R.Order.CurrentCMD);
 
 }
 public override void TurnStart()
 {
 R.Display.Print(R.Display.Format(R.Data));
 List<Factory> factories = R.Factories;
 FightSimulation.Result [] result = R.Wars.Simulate(factories);
 for (int i = 0; i < result.Length; i++)
 {
 R.Display.Print("ID("+factories[i]+ ")\n"+R.Display.Format(result[i]));
 }
 
 }
 public override void StartFirstTurn()
 {
 Factory s = R.StartOf(PlayerFaction.Ally);
 List<Factory> bigResource = R.Factories.Where(f => f.IsNeutral && f.Units >= 8 && f.Production == 3 & R.Distance(f, s) < 4).ToList();
 if (bigResource.Count>0)
 {
 R.Nuke.LaunchTo(bigResource[0]);
 }
 }
 public override void Turn()
 {
 if (R.Turn == 2 && R.Nuke.LaunchedNuke.Count == 1)
 {
 Factory r = R.Nuke.LaunchedNuke[0].Destination;
 R.Order.AddMove(R.StartOf(PlayerFaction.Ally), r, R.Nuke.LeftEstimation(r) + 1);
 }
 List<Factory> myArmy = R.AllyFactories;
 for (int j = 0; j < myArmy.Count; j++)
 {
 Factory attacking = myArmy[j];//SF.GetMyMostProtectedFactory();
 List<Factory> factoryNeighbour = R.Nearest(attacking);
 factoryNeighbour = R.Keep(factoryNeighbour, Factions.Neutral);
 factoryNeighbour = SF.RemoveUnproductifFactory(factoryNeighbour);
 if (factoryNeighbour.Count <= 0)
 {
 factoryNeighbour = R.Nearest(attacking);
 factoryNeighbour = R.Keep(factoryNeighbour, Factions.Enemy);
 factoryNeighbour = SF.RemoveUnproductifFactory(factoryNeighbour);
 }
 if (factoryNeighbour == null || factoryNeighbour.Count == 0)
 continue;
 Factory attacked = factoryNeighbour[0];
 int arriving = R.Distance(attacking, attacked);
 FightSimulation.Result rs = R.Wars.Simulate(attacked, arriving);
 int unitNeed = rs.UnitAt(arriving)+1;
 D.L(string.Format("Need at {0} {1}", attacked, unitNeed));
 if (unitNeed > attacked.Units)
 {
 int count = unitNeed; //- (attacking.Production == 0 ? 0 : 3);
 R.Order.AddMove(attacking, attacked, Math.Max(count, 0));
 }
 }
 
 if (R.Nuke.HaveNuke)
 {
 Troop nukable = SF.GetNukeCandidate();
 if (nukable!=null) {
 Factory d = nukable.Destination;
 R.Nuke.LaunchTo(d);
 }
 }
 }
 public class Wars
 {
 static int _ourBombsLeft = 2;
 static int _enemyBombsLeft = 2;
 static int[] _unityCount = new int[2];
 static int[] _factoryIncome = new int[2];
 public static int GetBombLeft(PlayerFaction team) { return team == PlayerFaction.Ally ? _ourBombsLeft : _enemyBombsLeft; }
 public static void SetUnitTo(int score, PlayerFaction team)
 {
 _unityCount[GetTeamIndex(team)] = score;
 }
 public static void SetIncomeTo(int totalIncome, PlayerFaction team)
 {
 _factoryIncome[GetTeamIndex(team)] = totalIncome;
 }
 public static int GetUnityOf(PlayerFaction team) { return _unityCount[GetTeamIndex(team)]; }
 public static int GetIncomeOf(PlayerFaction team) { return _factoryIncome[GetTeamIndex(team)]; }
 public static float GetUnitDiff() { return _unityCount[0] - _unityCount[1]; }
 public static float GetIncomeDiff() { return _factoryIncome[0] - _factoryIncome[1]; }
 private static int GetTeamIndex(PlayerFaction team) { return team == PlayerFaction.Ally ? 0 : 1; }
 public static bool IsInDanger(Factory factory)
 {
 return SF.GetTroopsTargeting(factory, PlayerFaction.Ally).Count > 0;
 }
 public static void RemoveNuke(PlayerFaction team)
 {
 if (team == PlayerFaction.Ally)
 _ourBombsLeft--;
 else _enemyBombsLeft--;
 if (_ourBombsLeft < 0) _ourBombsLeft = 0;
 if (_enemyBombsLeft < 0) _enemyBombsLeft = 0;
 }
 public static void LaunchAtomicNuke(Factory target)
 {
 if (Wars.GetBombLeft(PlayerFaction.Ally) <= 0)
 return;
 List<Factory> nearest = R.Data.GetNearestFactoriesOf(target.ID);
 foreach (Factory f in nearest)
 {
 if (f.IsAlly && f.ID != target.ID)
 {
 R.Order.AddBombAttack(f, target);
 Wars.RemoveNuke(PlayerFaction.Ally);
 Debug.Log("What ?!!!");
 }
 }
 }
 }
}
internal class MapStat :Singleton<MapStat>
{
 Dictionary<int, FightWithReinforcementSimulation> _reinforcementNeed = new Dictionary<int, FightWithReinforcementSimulation>();
 public void SetReinforcementNeed(Factory factory, FightWithReinforcementSimulation reinforcementNeed) {
 _reinforcementNeed[factory.ID] = reinforcementNeed;
 }
 public FightWithReinforcementSimulation GetRequiredTroops(Factory factory) {
 if (_reinforcementNeed.ContainsKey(factory.ID))
 return _reinforcementNeed[factory.ID];
 return null;
 }
}
internal class Day5_DijsktratTest : TurnManagerAbstract
{
 public override void StartFirstTurn()
 {
 }
 public override void Turn()
 {
 List<FightSimulation> sims = R.Simulation.Where(s => s.Factory.IsAlly).ToList() ;
 foreach (FightSimulation s in sims)
 {
 FightSimulation.TurnResult nextTurn = s.LastResult.GetTurn(2);
 if (nextTurn.FactoryFightWinner == Owner.Enemy) {
 Claim c = new Claim("Defense of "+s.Factory.ID , s.Factory, R.Turn, s.Factory.Units, ClaimPriority.NotNegociable);
 R.Claim.AddClaim(c);
 }
 }
 foreach (Factory f in R.AllyFactories)
 {
 int units = R.Claim.UnitNotClaimed(f);
 Claim c = new Claim("Random Attack" , f, R.Turn, units, ClaimPriority.Low);
 R.Claim.AddClaim(c);
 }
 }
 public override void AfterExecuteTurn()
 {
 R.Display.Print(R.Claim);
 }
}
 class Day7_ClaimAI : TurnManagerAbstract
{
 
 public override void StartFirstTurn()
 {
 }
 public override void Turn()
 {
 foreach (FightSimulation r in R.Simulation.Where(x=>x.Factory.IsAlly))
 {
 R.Display.Print(R.Display.Format(r));
 }
 List<FightSimulation> sims = R.Simulation.Where(s => s.Factory.IsAlly).ToList();
 foreach (FightSimulation s in sims)
 {
 D.L("Def:" + s);
 FightSimulation.TurnResult nextTurn = s.LastResultNoProduction.GetTurn(2);
 if (nextTurn.FactoryFightWinner == Owner.Enemy)
 {
 R.Display.Print(R.Display.Format(s.LastResultNoProduction));
 Claim c = new Claim("Defense of " + s.Factory.ID, s.Factory, R.Turn, s.Factory.Units, ClaimPriority.NotNegociable);
 R.Claim.AddClaim(c);
 }
 }
 
 foreach (Factory f in R.AllyFactories)
 {
 int units = R.Claim.UnitNotClaimed(f);
 R.Claim.AddClaim(new Claim("Central station", f, R.Turn, units,ClaimPriority.Low));
 Factory target = GivenData.I.GetFactory(0);
 if (f!=target)
 R.Order.AddMove(f,target , units);
 }
 R.Display.Print(R.Claim);
 D.L(""+ R.Claim.existingClaim.Count);
 }
 private FightSimulation.TurnResult GetSimNeedOf(Factory factory, int turn, bool withProduction)
 {
 List<FightSimulation> sims = R.Simulation.Where(s => s.Factory==factory).ToList();
 if(!withProduction) return sims[0].LastResultNoProduction.GetTurn(turn);
 else return sims[0].LastResult.GetTurn(turn);
 }
}
public abstract class Commander {
}
static class D
{
 public static void L(string message) { Debug.Log(message); }
 public static void LE(string message) { Debug.LogError(message); }
}
static class Debug
{
 public static void LogError(string message) { Console.Error.WriteLine("ERROR:" + message); }
 public static void Log(string message, bool withStartLine = true) { Console.Error.WriteLine((withStartLine ? "> " : "") + message); }
}
class Display :Singleton<Display>
 {
 public void Print(string message)
 {
 string [] splited = message.Split('\n');
 for (int i = 0; i < splited.Length; i++)
 D.L(splited[i]);
 }
 
 public string Format(Troop o)
 {
 return string.Format("{0}: {1}- {2} in {3} ->{4}", o.ID, o.Origine, o.Units, o.TurnLeft, o.Destination);
 }
 
 public string Format(Troop[] o)
 {
 string d = string.Format(" \nTroops({0}):\n", o.Length);
 foreach (Troop t in o)
 {d += Format(t) +"\n";}
 return d;
 }
 public string Format(Path o)
 {
 return string.Format("{0}<-{1}->{2}", o.FactoryOne.ID, o.Distance, o.FactoryTwo.ID);
 }
 public string Format(Path [] o)
 {
 string d = string.Format(" \nPaths({0}):\n", o.Length);
 foreach (Path p in o)
 { d += Format(p) + "\n"; }
 return d;
 }
 public string Format(Factory o, bool plus=true)
 {
 string factory = string.Format("\nID {0}: {1}({2})", o.ID, o.Units, o.Production);
 if (!plus)
 return factory;
 string near = "";
 foreach (Factory f in R.Nearest(o))
 {
 near += string.Format(" {0}({1})", f.ID, R.Distance(o, f));
 }
 int ally, enemy;
 R.IncomingUnits(o, out ally, out enemy);
 return string.Format("{0} Near {1} A{2} E{3}) ", factory, near, ally, enemy);
 }
 public string Format(Factory[] o, bool plus = true)
 {
 string factories = "";
 foreach (Factory f in o)
 {
 factories += Format(o, true);
 }
 return string.Format("\n Factories({0})\nStart: {1}\n{2}", R.Data.GetFactoriesCount(), R.StartPath.Distance, factories);
 }
 public void Print(Arguments o) { Print(Format(o)); }
 public string Format(Arguments o,bool troops=false, bool nukes=true) {
 string r = "";
 for (int i = 0; i < o.Lines.Count; i++)
 {
 if ((nukes && o.Lines[i].Contains("BOMB")) || (troops && o.Lines[i].Contains("TROOP")))
 r += "-- " + o.Lines[i] + "\n";
 }
 return r;
 }
 public string Format(GivenData o)
 {
 string r="";
 r += string.Format(
 " GAME STATE: {0}/{1} Time:{2}({3:0.00}%)\n",
 R.Data.GetTurn() + 1, GivenData.MAXTURN, Timer.LastTurnDuration, Timer.LastTurnPourcentUsed);
 r += string.Format(
 " MAP DENSITY: {0}/{1}\n",
 R.Factories.Count, GivenData.MAXFACTORY);
 r += string.Format(
 " MAP SIZE: {0}/{1}\n",
 R.LongestPath.Distance, GivenData.MAXDISTANCE);
 r += string.Format(
 " RESOURCE DENSITY: {0}/{1}\n",
 R.CurrentProduction, R.MaxProduction);
 r += string.Format(
 " RESOURCE USED: A {0} N {1} E {2}\n",
 R.ProductionOf(Factions.Ally), R.ProductionOf(Factions.Neutral), R.ProductionOf(Factions.Enemy));
 r += string.Format(
 " STARTS PROXIMITY: ({0})\n",
 Format(R.StartPath));
 List<Troop> ally = R.AllyTroops, enemy = R.EnemyTroops;
 List<Factory> fally = R.AllyFactories , fneutral =R.Keep(R.Factories,Factions.Neutral), fenemy = R.EnemyFactories;
 
 r += string.Format(
 " WARS - TROOPS A {0}|{2} E{1}|{3} - FACTORY UNITY A {4} N {5} E{6}\n",
 ally.Count, enemy.Count, R.UnitsIn(ally), R.UnitsIn(enemy), fally.Sum(x => x.Units),fneutral.Sum(x => x.Units), fenemy.Sum(x => x.Units));
 r += string.Format(
 " WARS TOTALS: A {0} N {1} E{2}\n",
 R.UnitsIn(ally) + fally.Sum(x => x.Units), fneutral.Sum(x => x.Units), R.UnitsIn(enemy) + fenemy.Sum(x => x.Units));
 r += string.Format(
 " NUKE : {0}\n",
 Format(NukeManager.I));
 return r;
 }
 private object Format(NukeManager o)
 {
 string r = "";
 foreach (Nuke nuke in o.LaunchedNuke)
 {
 r += string.Format("\n N-{0} {1} ", nuke.ID, nuke.State(R.Turn)) ;
 }
 return r;
 }
 public void Print(FightSimulation o) { Print(Format(o)); }
 public string Format(FightSimulation o, bool plus = true)
 {
 string r = "Simuation on " + o.Factory;
 r += "With Defense:" + Format(o.LastResult);
 r += "Without defense:" + Format(o.LastResultNoProduction);
 return r;
 }
 public void Print(FightSimulation.Result o) { Print(Format( o)); }
 public string Format(FightSimulation.Result o, bool plus = true)
 {
 string result = string.Format("\nResult: Owner {0}|{1} Winner {2}|{3}", o.FirstTurn.Owner, o.FirstTurn.Unit, o.LastTurn.FactoryFightWinner, o.LastTurn.FactoryFightResult);
 if (!plus)
 return result;
 string turnDetail = "";
 foreach (FightSimulation.TurnResult t in o.Turns)
 turnDetail += Format(t) + "\n";
 return result + turnDetail;
 }
 public string Format(FightSimulation.TurnResult o) {
 return (string.Format("\nTurn {0} ||{1} -> {2} (A {3} vs E {4}) -> {7}({8}) = {5} with {6}", o.TurnIndex, o.Owner, o.Unit, o.IncomingAlly, o.IncomingEnemy, o.FactoryFightWinner, o.FactoryFightResult, o.IncomingFightWinner, o.IncomingTroopsFightResult));
 }
 public void Print(ClaimMap o) { Print(Format(o)); }
 public string Format(ClaimMap o)
 {
 List<Claim> claims = R.Claim.AllClaims();
 string map = string.Format("\nClaimed Map ({0}): ", claims.Count);
 foreach (Claim f in claims)
 {
 map += Format(f);
 }
 return map; 
 }
 public string Format(Claim o) {
 return (string.Format("\nClaim on {0} of {1} at {3} with {2} priority ", o.Where, o.RequiredUnit, o.Priority ,o.WhenToApply));
 }
}
public class Factory
{
 public Factory(int id)
 {
 _factoryID = id;
 }
 public void UpdateState(int owner, int affecteUnit, int production)
 {
 _owner = owner;
 _affectedUnit = affecteUnit;
 _factoryProduction = production;
 }
 int _factoryID;
 public int ID
 {
 get{
 return _factoryID;
 }
 }
 int _owner;
 public Owner Owner
 {
 get{
 if (_owner == 0) return Owner.Neutral;
 if (_owner == 1) return Owner.Ally;
 return Owner.Enemy;
 }
 }
 int _affectedUnit;
 public int Units
 {
 get
 {
 return _affectedUnit;
 }
 }
 int _factoryProduction;
 public int Production
 {
 get{
 return _factoryProduction;
 }
 }
 public bool IsAlly
 {
 get
 {
 return Owner == Owner.Ally;
 }
 }
 public bool IsEnemy
 {
 get
 {
 return Owner == Owner.Enemy;
 }
 }
 public bool IsNeutral
 {
 get
 {
 return Owner == Owner.Neutral;
 }
 }
 public bool Is(Factions selection) {
 bool result=false;
 switch (selection)
 {
 case Factions.Ally:
 if (IsAlly) result = true; break;
 case Factions.Enemy:
 if (IsEnemy) result = true; break;
 case Factions.Neutral:
 if (IsNeutral) result = true;
 break;
 case Factions.Players:
 if (IsAlly ||IsEnemy) result = true;
 break;
 case Factions.EnemyAndNeutral:
 if (IsEnemy || IsNeutral) result = true;
 break;
 case Factions.All:
 default:
 return false;
 }
 return result;
 }
 public static bool operator ==(Factory a, Factory b)
 {
 if (System.Object.ReferenceEquals(a, b))
 {
 return true;
 }
 
 if (((object)a == null) || ((object)b == null))
 {
 return false;
 }
 return a.ID == b.ID;
 }
 public static bool operator !=(Factory a, Factory b)
 {
 return !(a == b);
 }
 public override bool Equals(System.Object obj)
 {
 Factory p = obj as Factory;
 if ((object)p == null)
 {
 return false;
 }
 
 return base.Equals(obj) && p.ID == ID;
 }
 public bool Equals(Factory p)
 {
 return base.Equals((Factory)p) && p.ID == ID;
 }
 public override int GetHashCode()
 {
 return base.GetHashCode() ^ ID;
 }
 public override string ToString()
 {
 return "F(" + ID + ")";
 }
}
public class FightWithReinforcementSimulation {
 public int[] Reinforce { get; set; }
 public FightSimulation.Result Simulation { get; set; }
}
public class FightPredictionParams{
 int _factoryUnit;
 int _factoryProduction;
 Owner _owner;
 int [] _incomingAllyTroops;
 int [] _incomingEnemyTroops;
 int _totalTurn;
 public int TotalTurn { get { return _totalTurn; } }
 public Owner InitialOwner { get { return _owner; } }
 public int StartUnit { get { return _factoryUnit; } }
 public int Production { get { return _factoryProduction; } }
 
 public int GetUnits(int index, PlayerFaction team) {
 return index >= _incomingAllyTroops.Length ? 0 : team == PlayerFaction.Ally ? _incomingAllyTroops[index] : _incomingEnemyTroops[index];
 }
 public FightPredictionParams(Factory target) {
 _factoryProduction = target.Production;
 _factoryUnit = target.Units;
 _owner = target.Owner;
 
 List<Troop> enemy = SF.GetTroopsTargeting(target, PlayerFaction.Enemy);
 List<Troop> ally = SF.GetTroopsTargeting(target, PlayerFaction.Ally);
 
 _totalTurn = Math.Max(ally.Count>0?ally.Max(x => x.TurnLeft):0, enemy.Count>0? enemy.Max(x => x.TurnLeft):0);
 _incomingAllyTroops = new int[_totalTurn];
 for (int i = 0; i < ally.Count; i++)
 {
 _incomingAllyTroops[ally[i].TurnLeft - 1] = ally[i].Units;
 }
 _incomingEnemyTroops = new int[_totalTurn];
 for (int i = 0; i < enemy.Count; i++)
 {
 _incomingEnemyTroops[enemy[i].TurnLeft - 1] = enemy[i].Units;
 }
 }
 public string DisplayFormat()
 {
 return string.Format("Params: Last Attack in {0}: Defence({1}), Ally({2}), Enemy({3})", TotalTurn, StartUnit, GetIncomingUnits(PlayerFaction.Ally), GetIncomingUnits(PlayerFaction.Enemy));
 }
 private object GetIncomingUnits(PlayerFaction team)
 {
 int count = 0;
 if (team == PlayerFaction.Ally || team == PlayerFaction.Both)
 count += _incomingAllyTroops.Sum(x => x);
 if (team == PlayerFaction.Enemy || team == PlayerFaction.Both)
 count += _incomingEnemyTroops.Sum(x => x);
 return count;
 }
}
public class FightSimulation {
 private FightPredictionParams _iniParams;
 public FightPredictionParams Params { get { return _iniParams; } }
 public Result LastResult;
 public Result LastResultNoProduction;
 private Factory f;
 public Factory Factory { get { return f; } }
 public FightSimulation(Factory f)
 {
 SetParams(f);
 }
 public void SetParams(Factory f) {
 this. f = f;
 _iniParams = new FightPredictionParams(f);
 }
 #region OLD
 #endregion
 public Result Simulate(int turnNeeded, params int[] reinforcement)
 {
 LastResultNoProduction = Simulate(turnNeeded, false, reinforcement);
 return LastResult = Simulate(turnNeeded,true, reinforcement);
 }
 public Result Simulate(int turnNeeded,bool withProd=true, params int[] reinforcement)
 {
 int turnToSimulate = 0; 
 if (turnToSimulate <= reinforcement.Length)
 turnToSimulate = reinforcement.Length;
 if (turnToSimulate <= turnNeeded)
 turnToSimulate = turnNeeded+1;
 if (turnToSimulate <= Params.TotalTurn)
 turnToSimulate = Params.TotalTurn;
 TurnResult[] turnResult = new TurnResult[turnToSimulate];
 FightPredictionParams p = Params;
 TurnResult firstTurn = new TurnResult(0, p.StartUnit, withProd ? p.Production:0, p.InitialOwner);
 if (turnToSimulate <= 0)
 {
 firstTurn.SimulateFight();
 return new Result(new TurnResult[] { firstTurn });
 }
 int[] resizedArray = new int[turnToSimulate];
 if (reinforcement.Length <= turnToSimulate)
 {
 reinforcement.CopyTo(resizedArray, 0);
 }
 else {
 resizedArray = reinforcement.Take(turnToSimulate).ToArray();
 }
 reinforcement = resizedArray;
 firstTurn.SetIncomingTroops(p.GetUnits(0, PlayerFaction.Ally), reinforcement[0], p.GetUnits(0, PlayerFaction.Enemy));
 firstTurn.SimulateFight();
 turnResult[0] = firstTurn;
 TurnResult previousTurn = firstTurn;
 for (int i = 1; i < turnToSimulate; i++)
 {
 int producted = previousTurn.Owner == Owner.Neutral || !withProd ? 0 : p.Production;
 TurnResult turn = new TurnResult(i, previousTurn.FactoryFightResult+producted, withProd?p.Production:0, previousTurn.FactoryFightWinner);
 turn.SetIncomingTroops(p.GetUnits(i, PlayerFaction.Ally), reinforcement[i], p.GetUnits(i, PlayerFaction.Enemy));
 turn.SimulateFight();
 turnResult[i] = turn;
 previousTurn = turn;
 }
 return LastResult=new Result(turnResult);
 }
 
 public class Result {
 public TurnResult[] _turn;
 public Result(TurnResult[] result) {
 _turn = result;
 }
 public TurnResult GetResult(int index) { return _turn[index]; }
 public TurnResult LastTurn { get { return _turn[_turn.Length - 1]; } }
 public TurnResult FirstTurn { get { return _turn[0]; } }
 public int UnitAt(int turn) { return _turn[turn].FactoryFightResult; }
 public Owner OwnerAt(int turn) { return _turn[turn].FactoryFightWinner; }
 public Owner OwnerAtStart { get { return FirstTurn.FactoryFightWinner; } }
 public int UnitsAtStart { get { return FirstTurn.FactoryFightResult; } }
 public Owner Winner { get { return LastTurn.FactoryFightWinner; } }
 public int UnitsLeft { get { return LastTurn.FactoryFightResult; } }
 public IEnumerable<TurnResult> Turns { get { return _turn; } }
 public void Display() {
 D.L(DisplayFormat());
 for (int i = 0; i < _turn.Length; i++)
 {
 D.L(_turn[i].DisplayFormat());
 }
 }
 public string DisplayFormat()
 {
 return (string.Format("Simulation: (Owner:{0}|{1} Winner:{2}|{3}", FirstTurn.Owner, FirstTurn.Unit, LastTurn.FactoryFightWinner, LastTurn.FactoryFightResult )); 
 }
 internal TurnResult GetTurn(int turn)
 {
 if (turn <= 0 || turn >= _turn.Length)
 return null;
 return _turn[turn];
 }
 }
 public class TurnResult {
 public TurnResult(int index, int unit, int production, Owner owner) {
 TurnIndex = index;
 Unit = unit;
 Production = production;
 Owner = owner;
 }
 public void SetIncomingTroops(int allyUnits, int allyReinforcement, int enemyUnit) {
 IncomingAlly = allyUnits;
 Reinforcement = allyReinforcement;
 IncomingEnemy = enemyUnit;
 }
 public int TurnIndex;
 public int Unit;
 public int Production;
 public Owner Owner;
 public int IncomingAlly;
 public int IncomingEnemy;
 public int Reinforcement;
 
 public int IncomingTroopsFightResult;
 public PlayerFaction IncomingFightWinner;
 public int FactoryFightResult;
 public Owner FactoryFightWinner;
 public void SimulateFight() {
 SimulateTroopsBattle(out IncomingTroopsFightResult, out IncomingFightWinner, IncomingAlly, IncomingEnemy, Reinforcement);
 
 SimulatreFacotryBattle(IncomingTroopsFightResult, IncomingFightWinner, out FactoryFightResult, out FactoryFightWinner);
 }
 public void SimulateTroopsBattle(out int leftUnits, out PlayerFaction winner, int allyUnits, int enemyUnits, int reinforcement)
 {
 
 int fightResult = (allyUnits + reinforcement) - enemyUnits ;
 leftUnits = Math.Abs(fightResult);
 winner = fightResult >= 0 ? PlayerFaction.Ally : PlayerFaction.Enemy;
 }
 public void SimulatreFacotryBattle(int incomingUnits, PlayerFaction unitsFaction, out int unitLeft, out Owner newOwner)
 {
 if (incomingUnits == 0)
 {
 unitLeft = Unit;
 newOwner = Owner;
 }
 else { 
 unitLeft = 0;
 bool isReinforcement = (Owner == Owner.Ally && unitsFaction == PlayerFaction.Ally)
 || (Owner == Owner.Enemy && unitsFaction == PlayerFaction.Enemy);
 if (isReinforcement)
 {
 unitLeft = incomingUnits + Unit;
 newOwner = Owner;
 }
 else
 {
 int fightResult = Unit - incomingUnits;
 unitLeft = Math.Abs(fightResult);
 if (fightResult >= 0)
 newOwner = Owner;
 else newOwner = unitsFaction == PlayerFaction.Ally ? Owner.Ally : Owner.Enemy;
 }
 }
 }
 public string DisplayFormat()
 {
 return (string.Format("Turn {0}, Current:{1} with {2} (A {3} vs E {4}) Troops Fight {7}({8}) Future Owner: {5} with {6}", TurnIndex, Owner, Unit, IncomingAlly, IncomingEnemy , FactoryFightWinner, FactoryFightResult, IncomingFightWinner, IncomingTroopsFightResult));
 }
 
 }
}
public class Overwatch :Singleton<Overwatch>{
 public void SetUpWith(List<Factory> factories)
 {
 foreach (Factory f in factories)
 {
 _activities.Add(f.ID, new FactoryActivity(f));
 FightSimulation fight = new FightSimulation(f);
 _simulations.Add(f.ID, fight);
 _simulationsResult.Add(f.ID, fight.Simulate(5));
 }
 }
 public Dictionary<int, FactoryActivity> _activities = new Dictionary<int, FactoryActivity>();
 public List<FactoryActivity> GetActivities() { return _activities.Values.ToList(); }
 
 public Dictionary<int, FightSimulation> _simulations = new Dictionary<int, FightSimulation>();
 public Dictionary<int, FightSimulation.Result> _simulationsResult = new Dictionary<int, FightSimulation.Result>();
 public List<FightSimulation> GetSimulations() { return _simulations.Values.ToList(); }
 public List<FightSimulation.Result> GetSimResults() { return _simulationsResult.Values.ToList(); }
 public void SimulateAll(int miniTurn=5) {
 foreach (Factory f in R.Factories)
 {
 _simulationsResult[f.ID]= _simulations[f.ID].Simulate(miniTurn);
 }
 }
}
public class FactoryActivity
{
 public enum FactoryType { Unknow, Link, Leaf, BaseStation, MainBase, Defense }
 public FactoryType Type = FactoryType.Unknow;
 public Factory _linkedFactory;
 public Queue<int> _sendTroop =new Queue<int>(4);
 public Queue<int> _requiredTroop = new Queue<int>(4);
 public Queue<int> _lastUnityValue = new Queue<int>(4);
 private Factory f;
 public FactoryActivity(Factory f)
 {
 _linkedFactory = f;
 for (int i = 0; i < 4; i++)
 {
 _sendTroop.Enqueue(0);
 _requiredTroop.Enqueue(0);
 _lastUnityValue.Enqueue(0);
 }
 }
 public int SendedTroop { get { return _sendTroop.Last(); } 
 private set { _sendTroop.Dequeue(); _sendTroop.Enqueue(value); } } 
 public int RequiredTroop
 {
 get { return _requiredTroop.Last(); }
 private set { _requiredTroop.Dequeue(); _requiredTroop.Enqueue(value); }
 }
 public int Units
 {
 get { return _lastUnityValue.Last(); }
 private set { _lastUnityValue.Dequeue(); _lastUnityValue.Enqueue(value); }
 }
 public double UnitsAverage
 {
 get { return _lastUnityValue.Average(); }
 }
 public double SendAverage
 {
 get { return _lastUnityValue.Average(); }
 }
 public double RequiredAverage
 {
 get { return _requiredTroop.Average(); }
 }
 public bool IsUnitStable { get { return _lastUnityValue.All(x => x == UnitsAverage); } }
 public void OnTurnStart()
 {
 int currentUnit = _linkedFactory.Units;
 _lastUnityValue.Dequeue();
 _lastUnityValue.Enqueue(currentUnit);
 List<Troop> troops = R.Troops;
 int st=0, rt=0;
 foreach (Troop t in troops)
 {
 if (t.IsNew && t.Origine == _linkedFactory) st++;
 if (t.IsNew && t.Destination == _linkedFactory) rt++;
 }
 SendedTroop=st;
 RequiredTroop=rt;
 Type = DefineFactoryType();
 }
 public FactoryType DefineFactoryType() {
 double st = SendAverage, rt = RequiredAverage;
 double ratio = st / rt;
 if (Units > 30.0) return FactoryType.MainBase;
 if (Units > 20.0) return FactoryType.BaseStation;
 if (ratio > 5.0) return FactoryType.Leaf;
 if (ratio < 0.8) return FactoryType.Defense;
 return FactoryType.Link;
 }
}
public class Filter
{
 public static List<Factory> GetEnemy( List<Factory> factories)
 {
 return SF.KeepFactories(factories,Factions.Enemy);
 }
 public static List<Troop> GetEnemy(List<Troop> troops)
 {
 return SF.RemoveTroops(troops,PlayerFaction.Ally);
 }
}
public class GivenData : Singleton<GivenData>
{
 public const int MINFACTORY = 7;
 public const int MAXFACTORY = 15;
 public const int MINLINK = 21;
 public const int MAXLINK = 105;
 public const int MINDISTANCE = 1;
 public const int MAXDISTANCE = 20;
 public const int MAXTURN = 200;
 int _factoryCount = 0;
 Dictionary<int, Factory> _factoriesList = new Dictionary<int, Factory>();
 int _pathCount = 0;
 List<Path> _pathsList = new List<Path>();
 List<Nuke> _nukeList = new List<Nuke>();
 Dictionary<int, Troop> _troopCurrentTurn = new Dictionary<int, Troop>();
 Dictionary<int, Troop> _troopCurrentPreviousTurn = new Dictionary<int, Troop>();
 Factory _enemyStart;
 Factory _ourStart;
 public void SetInitialData(int factoryCount, int pathCount)
 {
 _factoryCount = factoryCount;
 _factoriesList = new Dictionary<int, Factory>();
 _pathCount = pathCount;
 _pathsList = new List<Path>();
 }
 
 public void SetStartup(Factory factory, PlayerFaction team)
 {
 if (team == PlayerFaction.Ally)
 _ourStart = factory;
 else
 _enemyStart = factory;
 }
 public Factory GetStartup(PlayerFaction team)
 {
 return team == PlayerFaction.Ally ? _ourStart : _enemyStart;
 }
 public void AddPath(int factoryIdFrom, int factoryIdTo, int distance)
 {
 _pathsList.Add(
 new Path(factoryIdFrom, factoryIdTo, distance)
 );
 }
 public Path GetLink(int linkId) { throw new Exception("TODO LATER"); }
 public List<Path> GetPossibleLinks(int factoryId) { throw new Exception("TODO LATER"); }
 public int GetFactoryCount() { return _factoryCount; }
 public int GetPathCount() { return _pathCount; }
 public Factory GetFactory(int factoryID)
 {
 if (_factoriesList.Count <= 0)
 Debug.LogError(" The Factories are not initialized. Please call the methode SetInitialData(..)");
 return _factoriesList[factoryID];
 }
 public void AddFactory(int entityId)
 {
 _factoriesList.Add(entityId, new Factory(entityId));
 }
 public List<Factory> GetFactories() { return new List<Factory>(_factoriesList.Values); }
 internal void AddNukeLaunch(int entityId, int owner, int from, int to, int turnLeft)
 {
 if ( ! _nukeList.Any(item => item.ID ==entityId)) { 
 _nukeList.Add(new Nuke(entityId, owner, from, to, turnLeft, GetTurn()));
 }
 }
 public List<int> GetFactoriesID() { return new List<int>(_factoriesList.Keys); }
 public void SetFactoryStateTo(int entityId, int owner, int affectedUnit, int production)
 {
 Factory fact = GetFactory(entityId);
 fact.UpdateState(owner, affectedUnit, production);
 }
 public void AddTroopToCurrentTurn(int entityId, int owner, int from, int to, int units, int turnLeft)
 {
 Troop troop = new Troop(entityId,R.Turn, turnLeft);
 troop.SetTroopStateWithArguments(owner, from, to, units, turnLeft);
 _troopCurrentTurn.Add(entityId, troop);
 }
 int _turnIndex = -1;
 public int GetTurn() { return _turnIndex; }
 public void SetAsNewTurn()
 {
 _turnIndex++;
 _troopCurrentPreviousTurn = _troopCurrentTurn;
 _troopCurrentTurn = new Dictionary<int, Troop>();
 }
 public int GetPathsCount()
 {
 return _pathsList.Count;
 }
 public int GetTroopsCount()
 {
 return _troopCurrentTurn.Keys.Count;
 }
 public int GetFactoriesCount()
 {
 return _factoriesList.Keys.Count;
 
 }
 public List<Troop> GetTroops()
 {
 return new List<Troop>(_troopCurrentTurn.Values);
 }
 public List<Troop> GetTroops(PlayerFaction team)
 {
 return (from t in new List<Troop>(_troopCurrentTurn.Values)
 where t.Is(team)
 select t).ToList();
 }
 public List<Path> GetPaths()
 {
 return _pathsList;
 }
 public Dictionary<int, List<Factory>> _factorySortedByNearestDistance = new Dictionary<int, List<Factory>>();
 public void FirstTurnComputation()
 {
 List<Factory> factories = GetFactories();
 for (int i = 0; i < factories.Count; i++)
 {
 int id = factories[i].ID;
 List<Factory> factoriesSorted = SF.GetNeightbourSortByDistance(id);
 _factorySortedByNearestDistance.Add(id, factoriesSorted);
 }
 }
 internal void StopRecordingTurnTime()
 {
 throw new NotImplementedException();
 }
 public List<Factory> GetNearestFactoriesOf(int factoryId)
 {
 return _factorySortedByNearestDistance[factoryId];
 }
 public Path PathOf(int factId1, int factId2)
 {
 if (factId1 == factId2)
 return null;
 foreach (Path p in _pathsList)
 {
 if (p.Contain(factId1, factId2))
 return p;
 }
 return null;
 }
 public int GetDistanceBetween(Factory factId1, Factory factId2) {
 return GetDistanceBetween(factId1.ID, factId2.ID);
 }
 public int GetDistanceBetween(int factId1, int factId2)
 {
 if (factId2 == factId1)
 return 0;
 return PathOf(factId1, factId2).Distance;
 }
 public int DistanceBetweenStartup()
 {
 return GetDistanceBetween(_enemyStart, _ourStart);
 }
 
 public List<Factory> GetEmpties()
 {
 List<Factory> f = GetFactories();
 return SF.RemoveUnproductifFactory(f, true);
 }
 public Factory GetEqualsDeparture(Factory target, int turnDuration)
 {
 List<Factory> factories = I.GetNearestFactoriesOf(target.ID);
 for (int i = 0; i < factories.Count; i++)
 {
 if (factories[i].IsAlly && I.GetDistanceBetween(target.ID, factories[i].ID) == turnDuration) {
 return factories[i];
 }
 }
 return null;
 }
 
}
public class Timer
{
 public static long StartGame;
 public static long StartTurn;
 public static long MaxTimeByTurn = 100000000;
 public static long Time { get { return DateTime.Now.Ticks; } }
 public static long TimeSinceStart { get { return Time - StartGame; } }
 public static long TimeSinceStartTurn { get { return Time - StartTurn; } }
 public static long LastTurnDuration { get; private set; }
 public static double LastTurnPourcentUsed { get { return ((double)LastTurnDuration) / ((double)MaxTimeByTurn); } }
 public static void SetGameStart() { StartGame = Time; }
 public static void SetTurnStart() { StartTurn = Time; }
 public static void SetTurnEnd() { LastTurnDuration = Time - StartTurn; }
}
public enum PlayerFaction { Ally, Enemy , Both }
public enum Factions { Ally, Enemy, Neutral, Players, All, EnemyAndNeutral }
public enum Owner { Ally, Neutral, Enemy }
public enum ProductionLevel:int { None = 0, Small = 1, Medium = 2, Big = 3}
public enum FightWinner { None, Ally, Neutral, Enemy }
public enum MapProductionState :int { Empty=0, AlmostEmpty = 1, HalfEmpty = 2, AllFactoryWorking=3, AlmostFull = 4, Full = 5 }
public enum MapTerritoryState :int { Start = 0, FirstWaves = 1, AllFactoryTaken = 2, NoNeutralAnyMore = 3, Losing=9, Winning = 10 }
public enum MapType { Small, Normal, Large}
///# This applicaiton is designed in team and the code could be found 3 times on the servers.
///# CREDIT: Strée Eloi, Jonathan Leyen, Abigail B. Barias
///# We do not know if it is against the rules and if we could be ban for that.
///# If it is the case could you please contact us before the ban. Thanks ;)
///# jams.center@gmail.com - +32 (0) 488 757 684 - Strée Eloi - https://www.facebook.com/streeeloi
///# Git repo: https://github.com/JamsCenter/2017_02_25_GhostInTheShell
public class NukeManager : Singleton<NukeManager>
{
 public List<Nuke> LaunchedNuke { get; private set; }
 public int AllyNuke=2; public int EnemtNuke=2;
 public bool HaveNuke { get{ return AllyNuke >0; } }
 public List<Factory> HaveBeenTargeted = new List<Factory>();
 public NukeManager()
 {
 LaunchedNuke = new List<Nuke>();
 }
 public void DetectedNukeLunch(Nuke nuke)
 {
 if (nuke.IsAlly) AllyNuke--;
 else EnemtNuke--;
 LaunchedNuke.Add(nuke);
 }
 
 List<string> LastTurnBomb=new List<string>();
 internal void BombDetectedThisTurn(List<string> bombDetected)
 {
 foreach (string boomId in LastTurnBomb)
 {
 if (!bombDetected.Contains(boomId))
 NotifyBombExplosion(boomId);
 }
 LastTurnBomb = bombDetected;
 }
 private void NotifyBombExplosion(string boomId)
 {
 LaunchedNuke.Where(x => x.ID == int.Parse(boomId)).First().Exploded();
 }
 internal int LeftEstimation(Factory bigResource)
 {
 return Math.Max(10, bigResource.Units / 2);
 }
 internal bool HasBeenTargeted(Factory destination)
 {
 return LaunchedNuke.Where(l => l.Destination == destination).Count()>0;
 }
 internal bool LaunchTo(Factory d)
 {
 if (R.Nuke.HaveBeenTargeted.Contains(d))
 return false;
 R.Order.AddBombAttack(R.Keep(R.Nearest(d), Factions.Ally).First(),d);
 R.Nuke.HaveBeenTargeted.Add(d);
 return true;
 }
}
public class Nuke
{
 int _nukeId;
 bool _owner;
 int _factoryFrom;
 int _factoryTo;
 int _arrivingTurn;
 int _launchedTurn;
 public Nuke(int id, int owner, int from, int to, int turnLeft, int currentTurn)
 {
 _nukeId = id;
 _owner = owner == 1;
 _factoryFrom = from;
 _factoryTo = to;
 _arrivingTurn = currentTurn + turnLeft;
 _launchedTurn = currentTurn;
 PossibleTargets = SF.SortFactoryWithMostProductive(R.AllyFactories);
 }
 public List<Factory> PossibleTargets;
 public bool IsAlly { get { return _owner; } }
 public Factory Origine { get { return R.ID(_factoryFrom); } }
 public Factory Destination { get { return IsAlly ? R.ID(_factoryTo) : null; } }
 public int RepopulationTurn { get { return _arrivingTurn + 5; } }
 public int TurnLeft(int turn)
 {
 int turnleft = _arrivingTurn - turn;
 return turnleft < 0 ? 0 : turnleft;
 }
 public enum NukeState { Incoming, NextTurn, Boom, Radiation, Outdated }
 public NukeState State(int turn)
 {
 if (turn < _arrivingTurn - 1) return NukeState.Incoming;
 if (turn > _arrivingTurn + 5) return NukeState.Outdated;
 if (turn > _arrivingTurn) return NukeState.Radiation;
 if (turn == _arrivingTurn) return NukeState.Boom;
 if (turn == _arrivingTurn - 1) return NukeState.NextTurn;
 return NukeState.Outdated;
 }
 public int ID
 {
 get
 {
 return _nukeId;
 }
 }
 public bool IsEnemy
 {
 get
 {
 return !IsAlly;
 }
 }
 public void Exploded () { _arrivingTurn = R.Turn; }
 public static bool operator ==(Nuke a, Nuke b)
 {
 if (System.Object.ReferenceEquals(a, b))
 {
 return true;
 }
 if (((object)a == null) || ((object)b == null))
 {
 return false;
 }
 return a.ID == b.ID;
 }
 public static bool operator !=(Nuke a, Nuke b)
 {
 return !(a == b);
 }
 public override bool Equals(System.Object obj)
 {
 Nuke p = obj as Nuke;
 if ((object)p == null)
 {
 return false;
 }
 return base.Equals(obj) && p.ID == ID;
 }
 public bool Equals(Nuke p)
 {
 return base.Equals((Nuke)p) && p.ID == ID;
 }
 public override int GetHashCode()
 {
 return base.GetHashCode() ^ ID;
 }
}
public class MoveOrder
{
 private int troupNumber;
 public MoveOrder(Factory from, Factory to, int units)
 {
 From = from;
 To = to;
 this.Units = units;
 }
 public Factory From { get; set; }
 public Factory To { get; set; }
 public int Units { get; set; }
}
public class OrderStack : Singleton<OrderStack>
{
 public List<string> actions = new List<string>();
 public List<MoveOrder> moves = new List<MoveOrder>();
 public void ClearAction() {
 actions.Clear();
 moves.Clear();
 }
 public int UnitsSendTo(Factory f) {
 return moves.Where(x => x.To == f).Sum(x => x.Units);
 }
 public void AddWait()
 {
 actions.Add("WAIT");
 }
 public void AddMove(Factory from, Factory to, int troupNumber)
 {
 actions.Add(string.Format("MOVE {0} {1} {2}", from.ID, to.ID, Math.Max(troupNumber, 0) ));
 moves.Add(new MoveOrder(from, to, troupNumber));
 }
 public void AddBombAttack(Factory from, Factory to)
 {
 actions.Add(string.Format("BOMB {0} {1}", from.ID, to.ID));
 }
 public void AddImprove(Factory factory )
 {
 actions.Add(string.Format("INC {0}", factory.ID));
 }
 public void AddMessage(string message)
 {
 actions.Add(string.Format("MSG {0} ",message));
 }
 public string CurrentCMD { get
 {
 string cmds = "";
 foreach (string cmd in actions)
 {
 cmds += cmd + ";";
 }
 if (cmds.Length > 0)
 cmds = cmds.Remove(cmds.Length - 1);
 else cmds = "Wait";
 return cmds;
 } }
 public void ExecuteAction()
 {
 string cmd = CurrentCMD;
 Console.WriteLine(cmd);
 Debug.Log("Request Sent: " + cmd);
 }
}
public class Path
{
 int _factoryOne;
 int _factoryTwo;
 int _distance;
 public Path(int fromFactory, int toFactory, int distance)
 {
 _factoryOne = fromFactory;
 _factoryTwo = toFactory;
 _distance = distance;
 }
 public Factory FactoryOne { get { return R.ID(_factoryOne); } }
 public Factory FactoryTwo { get { return R.ID(_factoryTwo); } }
 
 public Factory[] Factories { get { return new Factory[] { R.ID(_factoryOne), R.ID(_factoryTwo) }; } }
 public int Distance { get { return _distance; } }
 public bool Contain(int factoryId)
 {
 return _factoryOne == factoryId || _factoryTwo == factoryId;
 }
 public Factory GetOtherSide(int factId)
 {
 return _factoryOne == factId ? FactoryTwo : FactoryOne;
 }
 public bool Contain(int factId1, int factId2)
 {
 return (_factoryOne == factId1 && _factoryTwo == factId2) || (_factoryOne == factId2 && _factoryTwo == factId1);
 }
 public static bool operator ==(Path a, Path b)
 {
 if (System.Object.ReferenceEquals(a, b))
 {
 return true;
 }
 if (((object)a == null) || ((object)b == null))
 {
 return false;
 }
 return a.Contain(b._factoryOne, b._factoryTwo);
 }
 public static bool operator !=(Path a, Path b)
 {
 return !(a == b);
 }
 public override bool Equals(System.Object obj)
 {
 Path p = obj as Path;
 if ((object)p == null)
 {
 return false;
 }
 return base.Equals(obj) && p.Contain(_factoryOne, _factoryTwo);
 }
 public bool Equals(Path p)
 {
 return base.Equals((Path)p) && p.Contain(_factoryOne, _factoryTwo);
 }
 public override int GetHashCode()
 {
 return base.GetHashCode() ^ int.Parse(_factoryOne+"00"+_factoryTwo);
 }
 
}
 
class Player
{
 static void Main(string[] args)
 {
 Timer.SetGameStart();
 R.GameState = new Day7_ClaimAI();
 R.GameState.ReadInitialConsoleInput();
 string[] inputs;
 int factoryCount = R.Args.Add(int.Parse(Console.ReadLine()));
 int linkCount = R.Args.Add(int.Parse(Console.ReadLine()));
 R.Data.SetInitialData(factoryCount, linkCount);
 for (int i = 0; i < linkCount; i++)
 {
 inputs = R.Args.Add(Console.ReadLine()).Split(' ');
 int factory1 = int.Parse(inputs[0]);
 int factory2 = int.Parse(inputs[1]);
 int distance = int.Parse(inputs[2]);
 R.Data.AddPath(factory1, factory2, distance);
 }
 bool firstLoopRead=true;
 while (true)
 {
 Timer.SetTurnStart();
 
 R.Order.ClearAction();
 R.Data.SetAsNewTurn();
 R.GameState.BeforeTurnConsoleInput();
 int entityCount = R.Args.Add(int.Parse(Console.ReadLine())); // the number of entities (e.g. factories and troops)
 List<string> bombDetected = new List<string>();
 for (int i = 0; i < entityCount; i++)
 {
 inputs = R.Args.Add(Console.ReadLine()).Split(' ');
 
 int entityId = int.Parse(inputs[0]);
 string entityType = inputs[1];
 int arg1 = int.Parse(inputs[2]);
 int arg2 = int.Parse(inputs[3]);
 int arg3 = int.Parse(inputs[4]);
 int arg4 = int.Parse(inputs[5]);
 int arg5 = int.Parse(inputs[6]);
 if (firstLoopRead)
 {
 if (entityType == "FACTORY")
 {
 R.Data.AddFactory(entityId);
 }
 
 }
 if (entityType == "FACTORY")
 {
 R.Data.SetFactoryStateTo(entityId, arg1, arg2, arg3);
 }
 if (entityType == "TROOP")
 {
 R.Data.AddTroopToCurrentTurn(entityId, arg1, arg2, arg3, arg4, arg5);
 }
 if (entityType == "BOMB")
 {
 bombDetected.Add(entityType);
 if (arg1 == -1)
 {
 R.Data.AddNukeLaunch(entityId, arg1, arg2, arg3, arg4) ;
 }
 }
 }
 
 NukeManager.I.BombDetectedThisTurn(bombDetected);
 bombDetected.Clear();
 R.Display.Print(R.Args);
 R.Args.Clear();
 #region INITIALISATION AT FIRST TURN
 R.Claim.SetUnitsAvaialaible(R.Factories);
 R.Order.AddMessage("http://discord.gg/2JCAPnX");
 
 firstLoopRead = false;
 if (R.Data.GetTurn() == 0)
 {
 R.Data.FirstTurnComputation();
 List<Factory> factory = R.Data.GetFactories();
 Factory enemy = SF.KeepFactories(factory, Factions.Enemy)[0];
 Factory ally = SF.KeepFactories(factory, Factions.Ally)[0];
 R.Data.SetStartup(enemy, PlayerFaction.Enemy);
 R.Data.SetStartup(ally, PlayerFaction.Ally);
 foreach (FactoryActivity fa in R.Activities)
 {
 fa.OnTurnStart();
 }
 }
 R.Overwatch.SetUpWith(R.Factories);
 foreach (FactoryActivity fa in R.Activities)
 {
 fa.OnTurnStart();
 }
 R.Overwatch.SimulateAll();
 #endregion
 if (R.Turn == 0) {
 R.GameState.Start();
 R.GameState.StartFirstTurn();
 }
 if (R.Turn == 200) {
 R.GameState.StartOfLastTurn();
 }
 
 R.GameState.TurnStart();
 R.GameState.Turn();
 if (R.Turn == 0)
 {
 R.GameState.EndingOfFirstTurn();
 }
 if (R.Turn == 200)
 {
 R.GameState.EndingOfLastTurn();
 
 }
 R.GameState.TurnEnd();
 R.Claim.SetAllowedClaim();
 R.GameState.BeforeExecuteTurn();
 R.Order.ExecuteAction();
 R.GameState.ExecuteTurn();
 R.GameState.AfterExecuteTurn();
 if (R.Turn >= 200)
 {
 R.GameState.End();
 }
 Timer.SetTurnEnd();
 }
 }
}
 class R
{
 #region ALIAS OF VERY USED CLASS;
 public static GivenData Data { get { return GivenData.I; } }
 public static OrderStack Order { get { return OrderStack.I; } }
 public static Display Display { get { return Display.I; } }
 public static Overwatch Overwatch { get { return Overwatch.I; } }
 public static WarsSimulator Wars { get { return WarsSimulator.I; } }
 public static Arguments Args { get { return Arguments.I; } }
 public static TurnManagerInterface GameState = new TurnManagerNewbie();
 #endregion
 #region QUICK ACCESS TO VERY USED DATA;
 public static List<Troop> Troops { get { return Data.GetTroops(); } }
 public static List<Factory> Factories { get { return Data.GetFactories(); } }
 public static List<FactoryActivity> Activities { get { return Overwatch.GetActivities(); } }
 public static List<Factory> EnemyFactories { get { return Filter.GetEnemy(Factories); } }
 public static List<Troop> EnemyTroops { get { return Filter.GetEnemy(Troops); } }
 public static List<Factory> AllyFactories { get { return SF.KeepFactories(R.Factories, Factions.Ally); } }
 public static List<FightSimulation> Simulation { get { return Overwatch.GetSimulations(); } }
 public static List<FightSimulation.Result> SimuResults { get { return Overwatch.GetSimResults(); } }
 internal static void IncomingUnits(Factory o, out int ally, out int enemy)
 {
 SF.GetIncomingTroops(R.Troops, o, out ally, out enemy);
 }
 public static List<Troop> AllyTroops { get { return SF.KeepTroops(R.Troops, PlayerFaction.Ally); } }
 
 public static List<Path> Paths { get { return Data.GetPaths(); } }
 public static int Turn { get { return GivenData.I.GetTurn(); } }
 public static Factory RandomEnemy { get { return SF.GetRandomInRange(EnemyFactories); } }
 public static Path StartPath { get { return Data.PathOf(StartOf(PlayerFaction.Ally).ID, StartOf(PlayerFaction.Enemy).ID);} }
 public static Path LongestPath { get { return R.Paths.OrderByDescending(x => x.Distance).First(); } }
 public static int CurrentProduction { get { return R.Factories.Sum(f=>f.Production); } }
 public static int MaxProduction { get { return R.Factories.Count * 3; } }
 public static NukeManager Nuke { get { return NukeManager.I; } }
 public static ClaimMap Claim { get { return ClaimMap.I; } }
 public static List<Factory> Nearest(Factory factory) { return SF.GetNeightbourSortByDistance(factory); }
 #endregion
 #region QUICK ACCESS TO VERY USED METHODS
 public static Factory ID(int factoryID)
 {
 return Data.GetFactory(factoryID);
 }
 public static int Distance(Factory f1, Factory f2) {
 return Data.GetDistanceBetween(f1, f2);
 }
 public static Factory StartOf(PlayerFaction team) {
 return Data.GetStartup(team);
 }
 internal static int UnitsIn(List<Troop> list)
 { return SF.UnitsIn(list); }
 internal static int UnitsIn(List<Factory> list)
 { return SF.UnitsIn(list); }
 internal static List<Factory> Keep(List<Factory> factories, Factions team)
 {
 return SF.KeepFactories(factories, team);
 }
 internal static List<Factory> Remove(List<Factory> factories, Factions team)
 {
 return SF.RemoveFactories(factories, team);
 }
 internal static List<Troop> Keep(List<Troop> factories, PlayerFaction team)
 {
 return SF.KeepTroops(factories, team);
 }
 internal static List<Troop> Remove(List<Troop> factories, PlayerFaction team)
 {
 return SF.KeepTroops(factories, team);
 }
 internal static int ProductionOf(List<Factory> list)
 { return SF.ProductionOf(list); }
 internal static int ProductionOf(Factions faction)
 {
 return R.Factories.Where(f=>f.Is(faction)).Sum(f=>f.Production);
 }
 internal static List<Factory> Neighbour(Factory current, out int minRange)
 {
 return SF.GetNeighbour(R.Factories, current, out minRange);
 }
 #endregion
}
static class SF
{
 #region GETTER
 public static Factory GetRandomFactory(Factions selection)
 {
 return GetRandomInRange(KeepFactories(R.Factories, selection));
 }
 public static Factory GetMyMostProtectedFactory()
 {
 return GetAllianceFactories()[0];
 }
 internal static List<Troop> GetTroopFrom(Factory f, PlayerFaction faction)
 {
 return R.Troops.Where(t => t.Is(faction) && t.Origine==f).ToList();
 }
 public static List<Factory> GetAllianceFactories()
 {
 return (from factory in R.Factories
 where factory.IsAlly
 orderby factory.Units descending
 select factory).ToList();
 }
 public static List<Factory> GetNeightbourSortByDistance(Factory factory)
 {
 return GetNeightbourSortByDistance(factory.ID);
 }
 public static List<Factory> GetNeightbourSortByDistance(int factoryId)
 {
 return (from path in R.Paths
 where path.Contain(factoryId)
 orderby path.Distance ascending
 select path.GetOtherSide(factoryId)).ToList() ;
 }
 public static List<Troop> GetTroopsTargeting(Factory target, PlayerFaction faction)
 {
 List<Troop> tr = KeepTroops( R.Troops, faction);
 
 return (from t in tr
 where t.Destination == target
 select t).ToList();
 }
 public static Factory GetRandomInRange(List<Factory> factories)
 {
 if (factories == null || factories.Count == 0)
 return null;
 return factories[new Random().Next(0, factories.Count - 1)];
 }
 public static int GetIncomingTroops(List<Troop> troops, Factory target, out int enemyCount, out int allyCount)
 {
 enemyCount = 0;
 allyCount = 0;
 foreach (Troop troop in troops)
 {
 if (troop.Destination == target)
 {
 if (troop.IsAlly)
 allyCount++;
 else
 enemyCount++;
 }
 }
 return enemyCount + allyCount;
 }
 public static List<Factory> GetFactoriesByProductionLevel(List<Factory> factoryList, Factions teamSelection, ProductionLevel productionLevel)
 {
 return
 (
 (
 from factory in factoryList
 where factory.Is(teamSelection) && factory.Production == (int) productionLevel
 select factory
 ).ToList()
 );
 }
 internal static List<Troop> GetNukeCandidates()
 {
 List<Troop> bigFish = R.EnemyTroops.Where(x => x.Units >= 15 && x.Destination.Production > 1 && !NukeManager.I.HasBeenTargeted(x.Destination)).ToList();
 if (bigFish.Count > 0) return bigFish;
 else return R.EnemyTroops.Where(x => x.Destination.Production == 3 && !NukeManager.I.HasBeenTargeted(x.Destination)).ToList();
 }
 internal static Troop GetNukeCandidate()
 {
 if (R.Turn < 10)
 {
 List<Troop> tr = R.EnemyTroops.Where(
 x => x.Destination.Production == 3
 && !NukeManager.I.HasBeenTargeted(x.Destination)
 && !x.Destination.IsAlly).ToList();
 return tr.Count > 0 ? tr.First() : null;
 }
 else { 
 List<Troop> tr = R.EnemyTroops.Where(
 x => x.Origine.Production == 3 
 && !NukeManager.I.HasBeenTargeted(x.Destination)
 && !x.Destination.IsAlly).OrderByDescending(x=>x.Origine.Units).ToList();
 return tr.Count > 0 ? tr.First() : null;
 }
 }
 public static List<Factory> ReashableBy(Factory d, int turnLeft)
 {
 return GetNeightbourSortByDistance(d).Where(f => f.IsAlly && R.Distance(f, d) == turnLeft).ToList();
 }
 #endregion
 #region FILTER & SORT
 public static List<Factory> SortFactoryWithMostUnits(List<Factory> factoryNeighbour)
 {
 return (from factory in factoryNeighbour
 orderby factory.Units descending
 select factory).ToList();
 }
 public static List<Factory> SortFactoryWithLessUnits(List<Factory> factoryNeighbour)
 {
 return (from factory in factoryNeighbour
 orderby factory.Units ascending
 select factory).ToList();
 }
 internal static int Lenght(List<Factory> fp)
 {int c=fp.Count;if(c<=2)return 0;int l=0;Factory p=fp[0];for (int i=1;i<c;i++){l+=R.Distance(p, fp[i]);p =fp[i];}return l;}
 public static List<Factory> SortFactoryWithMostProductive(List<Factory> f)
 {
 return (from factory in f
 orderby factory.Production descending
 select factory).ToList();
 }
 
 public static List<Troop> SortTroopsByBestArmy(List<Troop> enemyTroops, int minArmy = 0)
 {
 return (from t in enemyTroops where t.Units > minArmy orderby t.Units descending select t).ToList();
 }
 #endregion
 #region KEEP & REMOVE
 public static List<Factory> RemoveFactories(List<Factory> factories, Factions toRemove, bool inverse=false)
 {
 return (from f in factories
 where inverse? (f.Is(toRemove)) : !(f.Is(toRemove))
 select f).ToList();
 
 }
 public static List<Factory> KeepFactories(List<Factory> factories, Factions toKeep)
 {
 return RemoveFactories(factories, toKeep, true);
 }
 internal static List<Factory> GetNeighbour(List<Factory> factories, Factory current, out int neighbourRange)
 {
 int range = 0;
 List<Factory> fact = GetNeightbourSortByDistance(current);
 if (fact.Count > 0) {
 range = R.Distance(fact[0],current);
 fact = (fact.Where(x => R.Distance(x, current)== range)).ToList();
 }
 neighbourRange = range;
 return fact;
 
 }
 public static List<Factory> RemoveUnproductifFactory(List<Factory> factories, bool inverse = false, int range=0)
 {
 return (from f in factories where (inverse ? f.Production <= range : f.Production > range) select f).ToList();
 }
 public static List<Troop> RemoveTroops(List<Troop> troop, PlayerFaction team, bool inverse=false)
 {
 return (from t in troop where inverse ? t.Is(team):!t.Is(team) select t).ToList();
 }
 public static List<Troop> KeepTroops(List<Troop> troop, PlayerFaction team)
 {
 return RemoveTroops(troop, team, true);
 }
 #endregion
 #region COMPUTE FONCTION 
 public static int UnitsIn(List<Troop> troops)
 {
 int count = 0;
 for (int i = 0; i < troops.Count; i++)
 {
 count += troops[i].Units;
 }
 return count;
 }
 public static int UnitsIn(List<Factory> factories)
 {
 int count = 0;
 for (int i = 0; i < factories.Count; i++)
 {
 count += factories[i].Units;
 }
 return count;
 }
 public static int ProductionOf(List<Factory> factories)
 {
 return factories.Sum(x => x.Production);
 }
 
 #endregion
}
public abstract class Singleton<T> where T : new()
{
 protected static T _singletonInstance;
 public static T I
 {
 get
 {
 if (_singletonInstance == null)
 _singletonInstance = new T();
 return _singletonInstance;
 }
 set { _singletonInstance = value; }
 }
}
public class Troop
{
 int _troopId;
 bool _owner;
 int _factoryFrom;
 int _factoryTo;
 int _unit;
 int _turnLeft;
 int _turnToArrived;
 int _turnCreated;
 int _turnDuration;
 public Troop(int troopId,int turnCreated, int turnLeft)
 {
 _troopId = troopId;
 _turnCreated = turnCreated;
 _turnToArrived = turnCreated + TurnLeft;
 _turnDuration = TurnLeft;
 }
 public void SetTroopStateWithArguments(int owner, int from, int to, int unit, int turnLeft)
 {
 _owner = owner == 1;
 _factoryFrom = from;
 _factoryTo = to;
 _unit = unit;
 _turnLeft = turnLeft;
 }
 public bool IsAlly { get { return _owner; } }
 public Factory Origine { get{ return R.ID(_factoryFrom); } }
 public Factory Destination { get { return R.ID(_factoryTo); } }
 public int Units { get { return _unit; } }
 public int TurnLeft { get{ return _turnLeft; } }
 public int TurnArriving { get { return _turnCreated + _turnLeft; } }
 public int TurnDone { get { return _turnDuration - _turnLeft; } }
 public int TurnDuration { get { return _turnDuration; } }
 public bool IsNew { get { return _turnDuration == _turnLeft; } }
 public int ID
 {get
 {
 return _troopId;
 }
 }
 public bool IsEnemy
 {
 get {
 return !IsAlly;
 }
 }
 public static bool operator ==(Troop a, Troop b)
 {
 if (System.Object.ReferenceEquals(a, b))
 {
 return true;
 }
 if (((object)a == null) || ((object)b == null))
 {
 return false;
 }
 return a.ID==b.ID;
 }
 public static bool operator !=(Troop a, Troop b)
 {
 return !(a == b);
 }
 public override bool Equals(System.Object obj)
 {
 Troop p = obj as Troop;
 if ((object)p == null)
 {
 return false;
 }
 return base.Equals(obj) && p.ID == ID;
 }
 public bool Equals(Troop p)
 {
 return base.Equals((Troop)p) && p.ID == ID;
 }
 public override int GetHashCode()
 {
 return base.GetHashCode() ^ ID;
 }
 public bool Is(PlayerFaction team)
 {
 bool result = false;
 switch (team)
 {
 case PlayerFaction.Ally:
 if (IsAlly) result = true;
 break;
 case PlayerFaction.Enemy:
 if (IsEnemy) result = true;
 break;
 case PlayerFaction.Both:
 result = true;
 break;
 default:
 break;
 }
 return result;
 }
}
public delegate void TurnCall();
public interface TurnManagerInterface {
 void ReadInitialConsoleInput();
 void Start();
 void BeforeTurnConsoleInput();
 void TurnStart();
 void StartFirstTurn();
 void StartOfLastTurn();
 void Turn();
 void EndingOfFirstTurn();
 void EndingOfLastTurn();
 void TurnEnd();
 void BeforeExecuteTurn();
 void ExecuteTurn();
 void AfterExecuteTurn();
 void End();
 void ScriptStart();
}
public class TurnManagerAbstract : TurnManagerInterface
{
 public virtual void ScriptStart() { }
 public virtual void ReadInitialConsoleInput() { }
 public virtual void Start() {}
 public virtual void BeforeTurnConsoleInput() { }
 public virtual void TurnStart() { }
 public virtual void StartFirstTurn() { }
 public virtual void StartOfLastTurn() { }
 public virtual void Turn() { }
 public virtual void EndingOfFirstTurn() { }
 public virtual void EndingOfLastTurn() { }
 public virtual void TurnEnd() { }
 public virtual void BeforeExecuteTurn() { }
 public virtual void ExecuteTurn() { }
 public virtual void AfterExecuteTurn() { }
 public virtual void End() { }
 
}
public class TurnManagerNewbie : TurnManagerAbstract {
 public override void Start()
 {
 Debug.Log("Hello World");
 }
 public override void TurnStart()
 {
 Debug.Log("Turn: " + R.Turn);
 }
 public override void Turn()
 {
 foreach (Factory f in SF.GetAllianceFactories())
 {
 for (int i = 0; i < f.Units - 1; i++)
 {
 Factory target = SF.GetRandomInRange(SF.RemoveUnproductifFactory(R.EnemyFactories));
 if (target != null)
 R.Order.AddMove(f, target, 1);
 }
 }
 }
 public override void ExecuteTurn()
 {
 Debug.Log(R.Order.CurrentCMD);
 }
}
class WarsSimulator: Singleton<WarsSimulator>
{
 public FightSimulation.Result Simulate(Factory f, int turnNeeded = -1)
 {
 FightPredictionParams paramsPrediciton;
 return Simulate(f, out paramsPrediciton,turnNeeded);
 }
 public FightSimulation.Result Simulate(Factory f, out FightPredictionParams parms, int turnNeeded=-1)
 {
 FightSimulation simulation = new FightSimulation(f);
 parms = simulation.Params;
 return simulation.Simulate( turnNeeded );
 }
 public FightSimulation.Result[] Simulate(List<Factory> f) {
 FightSimulation.Result[] result = new FightSimulation.Result[f.Count];
 for (int i = 0; i < f.Count; i++)
 {
 result[i] = Simulate(f[i]);
 }
 return result;
 }
}
