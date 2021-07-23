using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using Contracts.Agents;
using Strategies;
using KSP.Localization;

namespace funqt10ns {
    public class RAgency {
        public Agent agent;
        public int threshold;
        public bool met;
        public float rep;
        public RAgency(Agent agent, int threshold = -1, bool met = false, float rep = 0f) {
            this.agent = agent;
            this.threshold = threshold == -1 ? UnityEngine.Random.Range(0, 10) : threshold;
            this.met = met;
            this.rep = rep;
            if (threshold == -1)
                foreach (AgentMentality aMt in agent.Mentality)
                    if (aMt.DisplayName == "Stern") this.threshold += UnityEngine.Random.Range(3, 5);
                    else if (aMt.DisplayName == "EasyGoing") this.threshold = UnityEngine.Random.Range(0, 5);
        }
        public string GetSymbol() {
            if (agent.LogoURL == HighLogic.CurrentGame.flagURL) {
                float stMod = 0f;
                bool stModded = false;
                foreach (AgentStanding aS in agent.Standings) {
                    if (Carp.GetRAgency(aS.Agent).rep > aS.Standing * 10) {
                        stMod += aS.Standing;
                        stModded = true;
                    }
                }
                if (!stModded || stMod <= 1) return "+";
                else return "++";
            } else {
                RAgency fRA = Carp.FlagRAgency();
                if (fRA != null) {
                    foreach (AgentStanding aS in agent.Standings.Where(aS => aS.Agent == fRA.agent && fRA.rep > aS.Standing * 10))
                        if (aS.Standing < 0) return "<";
                        else if (aS.Standing > 1) return ">";
                        else return "";
                }
                return "";
            }
        }
        public void AddRep(float amount) {
            float prevRep;
            float modifiedAmount;
            foreach (RAgency rAgency in Carp.rAgencies)
                foreach (AgentStanding aSt in rAgency.agent.Standings.Where(aS => aS.Agent == agent))
                    if ((aSt.Standing < 0 || aSt.Standing > 1) && rAgency.rep > aSt.Standing * 10) {
                        prevRep = rAgency.rep;
                        modifiedAmount = amount * aSt.Standing / 4;
                        rAgency.rep += modifiedAmount;
                        Carp.onAgencyRepChanged.Fire(rAgency, modifiedAmount);
                        Debug.Log("carp: " + (aSt.Standing > 0 ? "+" : "") + modifiedAmount + " rep " + rAgency.agent.Name);
                        if (!rAgency.met && rAgency.rep >= threshold) {
                            rAgency.met = true;
                            Carp.onAgencyMet.Fire(rAgency);
                        } else if (prevRep <= rAgency.threshold && rAgency.rep > threshold) Carp.onThresholdMet.Fire(rAgency);
                        else if (prevRep > rAgency.threshold && rAgency.rep <= rAgency.threshold) Carp.onThresholdUnmet.Fire(rAgency);
                    }
            float stMod = 0f;
            bool stModded = false;
            foreach (AgentStanding aSt in agent.Standings)
                if (Carp.GetRAgency(aSt.Agent).rep > aSt.Standing * 10) {
                    stMod += aSt.Standing;
                    stModded = true;
                }
            prevRep = rep;
            modifiedAmount = stModded ? amount * Mathf.Clamp(stMod, 0.05f, 2) : amount;
            rep += modifiedAmount;
            Carp.onAgencyRepChanged.Fire(this, modifiedAmount);
            Debug.Log("carp: +" + modifiedAmount + " rep " + agent.Name);
            if (!met && rep >= threshold) {
                met = true;
                Carp.onAgencyMet.Fire(this);
            } else if (prevRep <= threshold && rep > threshold) Carp.onThresholdMet.Fire(this);
            else if (prevRep > threshold && rep <= threshold) Carp.onThresholdUnmet.Fire(this);
        }
    }
    [KSPScenario(ScenarioCreationOptions.AddToNewCareerGames | ScenarioCreationOptions.AddToExistingCareerGames, new GameScenes[] { GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER, GameScenes.EDITOR })]
    public class Carp : ScenarioModule {
        public static List<RAgency> rAgencies;
        private SortedDictionary<string, RAgency> rAgencyList;
        private bool popupActive;
        //private float prevRep;
        private static int rand;
        public void Start() {
            GameEvents.Contract.onCompleted.Add(ContractComplete);
            GameEvents.Modifiers.OnCurrencyModified.Add(ProgressCheck);
            //GameEvents.OnReputationChanged.Add(ProgressCheck);
            popupActive = false;
            //prevRep = Reputation.CurrentRep;
            SetNewRand();
            rAgencyList = new SortedDictionary<string, RAgency>();
            foreach (RAgency rAgency in rAgencies) rAgencyList.Add(rAgency.agent.Name, rAgency);
        }
        public void OnDisable() {
            GameEvents.Contract.onCompleted.Remove(ContractComplete);
            GameEvents.Modifiers.OnCurrencyModified.Remove(ProgressCheck);
            //GameEvents.OnReputationChanged.Remove(ProgressCheck);
        }
        public void Update() {
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER) {
                if (!popupActive && Mouse.screenPos.x >= (Screen.currentResolution.width * 0.488) && Mouse.screenPos.x <= (Screen.currentResolution.width * 0.564) && Mouse.screenPos.y <= 32) {
                    if (Input.GetMouseButtonDown(1)) popupActive = true;
                    List<DialogGUIBase> dialog = new List<DialogGUIBase>();
                    int rowCount = (int)Math.Ceiling(rAgencyList.Count * 0.5);
                    for (int i = 0; i < rowCount; i++) {
                        RAgency rAL = rAgencyList.ElementAt(i).Value;
                        if ((i == rowCount - 1) && (rAgencyList.Count % 2 != 0)) {
                            dialog.Add(new DialogGUIHorizontalLayout(
                                new DialogGUISpace(4),
                                new DialogGUIImage(new Vector2(64, 40), new Vector2(0, 0), Color.white, rAL.agent.LogoScaled),
                                new DialogGUIBox((Math.Round(rAL.rep * 100) * 0.01).ToString() + " " + rAL.GetSymbol(), 64, 40)
                                ));
                        } else {
                            RAgency rAR = rAgencyList.ElementAt(i + rowCount).Value;
                            dialog.Add(new DialogGUIHorizontalLayout(
                                new DialogGUISpace(4),
                                new DialogGUIImage(new Vector2(64, 40), new Vector2(0, 0), Color.white, rAL.agent.LogoScaled),
                                new DialogGUIBox((Math.Round(rAL.rep * 100) * 0.01).ToString() + " " + rAL.GetSymbol(), 64, 40),
                                new DialogGUISpace(4),
                                new DialogGUIImage(new Vector2(64, 40), new Vector2(0, 0), Color.white, rAR.agent.LogoScaled),
                                new DialogGUIBox((Math.Round(rAR.rep * 100) * 0.01).ToString() + " " + rAR.GetSymbol(), 64, 40)
                                ));
                        }
                    }
                    PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new MultiOptionDialog("carpPopup", "", Localizer.Format("#autoLOC_7001033") + ": " + Math.Round(Reputation.CurrentRep * 100) * 0.01 + "\n\n", HighLogic.UISkin, dialog.ToArray()),
                        false, HighLogic.UISkin);
                } else if (Input.GetMouseButtonDown(1)) popupActive = false;
                else if (!popupActive) PopupDialog.DismissPopup("carpPopup");
            }
        }
        public override void OnSave(ConfigNode save) {
            if (rAgencies != null)
                foreach (RAgency rAgency in rAgencies) {
                    ConfigNode rANode = save.AddNode("RAgency");
                    rANode.AddValue("name", rAgency.agent.Name);
                    rANode.AddValue("met", rAgency.met);
                    rANode.AddValue("rep", rAgency.rep);
                    rANode.AddValue("threshold", rAgency.threshold);
                }
        }
        public override void OnLoad(ConfigNode save) {
            if (rAgencies == null) {
                rAgencies = new List<RAgency>();
                foreach (ConfigNode rANode in save.GetNodes().Where(rAN => AgentList.Instance.GetAgent(rAN.GetValue("name")) != null))
                    rAgencies.Add(new RAgency(
                        AgentList.Instance.GetAgent(rANode.GetValue("name")),
                        rANode.HasValue("threshold") ? int.Parse(rANode.GetValue("threshold")) : -1,
                        rANode.HasValue("met") && bool.Parse(rANode.GetValue("met")),
                        rANode.HasValue("rep") ? float.Parse(rANode.GetValue("rep")) : 0
                    ));
                if (rAgencies.Count < AgentList.Instance.Agencies.Count)
                    foreach (Agent agent in AgentList.Instance.Agencies.Where(a => !rAgencies.Any(rA => rA.agent == a)))
                        rAgencies.Add(new RAgency(agent));
            }
        }
        public static EventData<RAgency> onAgencyMet = new EventData<RAgency>(nameof (onAgencyMet));
        public static EventData<RAgency> onThresholdMet = new EventData<RAgency>(nameof (onThresholdMet));
        public static EventData<RAgency> onThresholdUnmet = new EventData<RAgency>(nameof (onThresholdUnmet));
        public static EventData<RAgency, float> onAgencyRepChanged = new EventData<RAgency, float>(nameof(onAgencyRepChanged));
        public static RAgency GetRAgency(Agent agent) {
            foreach (RAgency rAgency in rAgencies.Where(rA => rA.agent == agent)) return rAgency;
            return null;
        }
        public static RAgency GetRAgency(string agentName) {
            foreach (RAgency rAgency in rAgencies.Where(rA => rA.agent.Name == agentName)) return rAgency;
            return null;
        }
        public static RAgency FlagRAgency() {
            foreach (RAgency rAgency in rAgencies.Where(rA => rA.agent.LogoURL == HighLogic.CurrentGame.flagURL)) return rAgency;
            return null;
        }
        public static RAgency RandRAgency() { return rAgencies[rand]; }
        public static void SetNewRand() { rand = UnityEngine.Random.Range(0, rAgencies.Count - 1); }
        private void ContractComplete(Contracts.Contract contract) { GetRAgency(contract.Agent).AddRep(contract.ReputationCompletion); }
        //public void ProgressCheck(float newRep, TransactionReasons reason) {
        //    RAgency fRA = FlagRAgency();
        //    if (reason == TransactionReasons.Progression && fRA != null) fRA.AddRep(newRep - prevRep);
        //    prevRep = Reputation.CurrentRep;
        //}
        private void ProgressCheck(CurrencyModifierQuery query) {
            RAgency fRA = FlagRAgency();
            if (query.reason == TransactionReasons.Progression && fRA != null) fRA.AddRep(query.GetTotal(Currency.Reputation));
        }
    }
    public class AddAgencyRepOnce : StrategyEffect {
        private Currency currency;
        private float rateMin;
        private float rateMax;
        private string agencyName;
        public AddAgencyRepOnce(Strategy parent, Currency currency, float rateMin, float rateMax, string agencyName) : base(parent) {
            this.currency = currency;
            this.rateMin = rateMin;
            this.rateMax = rateMax;
            this.agencyName = agencyName;
        }
        public AddAgencyRepOnce(Strategy parent) : base(parent) { }
        protected override void OnLoadFromConfig(ConfigNode node) {
            currency = node.HasValue("currency") ? (Currency)Enum.Parse(typeof(Currency), node.GetValue("currency")) : Currency.Funds;
            rateMin = node.HasValue("rateMin") ? float.Parse(node.GetValue("rateMin")) : 0f;
            rateMax = node.HasValue("rateMax") ? float.Parse(node.GetValue("rateMax")) : 1f;
            agencyName = node.HasValue("agencyName") ? node.GetValue("agencyName") : "random";
        }
        protected override string GetDescription() {
            if (agencyName == "flagAgencyOnly" && Carp.FlagRAgency() == null) return "";
            RAgency rAgency;
            if (agencyName.StartsWith("flag")) rAgency = Carp.FlagRAgency() ?? Carp.RandRAgency();
            else rAgency = Carp.GetRAgency(agencyName) ?? Carp.RandRAgency();
            return Localizer.Format("#autoLOC_900356") + ": " + rAgency.agent.Title;
        }
        protected override void OnRegister() {
            if (agencyName == "flagAgencyOnly" && Carp.FlagRAgency() == null) return;
            float total = 0f;
            RAgency rAgency;
            if (currency == Currency.Funds) total = (float)Funding.Instance.Funds;
            else if (currency == Currency.Science) total = ResearchAndDevelopment.Instance.Science;
            else if (currency == Currency.Reputation) total = Reputation.Instance.reputation;
            if (agencyName.StartsWith("flag")) rAgency = Carp.FlagRAgency() ?? Carp.RandRAgency();
            else rAgency = Carp.GetRAgency(agencyName) ?? Carp.RandRAgency();
            rAgency.AddRep(ParentLerp(0, total) * ParentLerp(rateMin, rateMax));
            if (rAgency.agent.Name == Carp.RandRAgency().agent.Name) Carp.SetNewRand();
        }
    }
    public class AddAgencyRepReasons : StrategyEffect {
        private Currency currency;
        private float rateMin;
        private float rateMax;
        private string reasons;
        private string description;
        private string agencyName;
        public AddAgencyRepReasons(Strategy parent, Currency currency, float rateMin, float rateMax, string reasons, string description, string agencyName) : base(parent) {
            this.currency = currency;
            this.rateMin = rateMin;
            this.rateMax = rateMax;
            this.reasons = reasons;
            this.description = description;
            this.agencyName = agencyName;
        }
        public AddAgencyRepReasons(Strategy parent) : base(parent) { }
        protected override void OnLoadFromConfig(ConfigNode node) {
            currency = node.HasValue("currency") ? (Currency)Enum.Parse(typeof(Currency), node.GetValue("currency")) : Currency.Funds;
            rateMin = node.HasValue("rateMin") ? float.Parse(node.GetValue("rateMin")) : 0f;
            rateMax = node.HasValue("rateMax") ? float.Parse(node.GetValue("rateMax")) : 1f;
            reasons = node.HasValue("reasons") ? node.GetValue("reasons") : "None";
            description = node.HasValue("description") ? node.GetValue("description") : Localizer.Format("#autoLOC_900356") + ": ";
            agencyName = node.HasValue("agencyName") ? node.GetValue("agencyName") : "random";
        }
        protected override string GetDescription() {
            if (agencyName == "flagAgencyOnly" && Carp.FlagRAgency() == null) return "";
            RAgency rAgency;
            if (agencyName.StartsWith("flag")) rAgency = Carp.FlagRAgency() ?? Carp.RandRAgency();
            else rAgency = Carp.GetRAgency(agencyName) ?? Carp.RandRAgency();
            return description + rAgency.agent.Title;
            // return "+" + Math.Round((ParentLerp(0, ResearchAndDevelopment.Instance.Science) / 20) * 100) * 0.01 + " " + Localizer.Format("#autoLOC_900356") + ": " + agent.Title;
            //RAgency fRA = Carp.FlagRAgency();
            //if (fRA != null) return "-" + Localizer.Format("#autoLOC_900356") + ": " + Localizer.Format("#autoLOC_442409") + " " + Localizer.Format("#autoLOC_7003010");
            //else return "";
        }
        protected override void OnRegister() => GameEvents.Modifiers.OnCurrencyModified.Add(AddRepFromQuery);
        protected override void OnUnregister() => GameEvents.Modifiers.OnCurrencyModified.Remove(AddRepFromQuery);
        protected virtual void AddRepFromQuery(CurrencyModifierQuery query) {
            if (reasons.Contains(query.reason.ToString()) && (agencyName != "flagAgencyOnly" || Carp.FlagRAgency() != null)) {
                RAgency rAgency;
                if (agencyName.StartsWith("flag")) rAgency = Carp.FlagRAgency() ?? Carp.RandRAgency();
                else rAgency = Carp.GetRAgency(agencyName) ?? Carp.RandRAgency();
                rAgency.AddRep(ParentLerp(0, query.GetTotal(currency)) * ParentLerp(rateMin, rateMax));
            }
        }
        //protected virtual void ContractComplete(Contracts.Contract contract) {
        //    Carp.GetRAgency(contract.Agent).AddRep(ParentLerp(0, contract.ReputationCompletion) * -1);
        //    // ParentLerp(0, ResearchAndDevelopment.Instance.Science) * ParentLerp(0.04, 0.05);
        //    // fRA.AddRep(ParentLerp(0, ResearchAndDevelopment.Instance.Science) / 20);
        //}
    }
}
