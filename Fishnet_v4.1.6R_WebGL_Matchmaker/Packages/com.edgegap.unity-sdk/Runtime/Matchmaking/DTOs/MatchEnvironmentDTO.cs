using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Edgegap.Matchmaking
{
    using Env = DeploymentEnvironmentDTO;

    public class MatchEnvironmentDTO<A>
    {
        public string MatchProfile;
        public List<string> TicketIds;
        public Dictionary<string, InjectedTicketDTO<A>> Tickets;
        public Dictionary<string, List<string>> Groups;
        public Dictionary<string, List<string>> Teams;
        public string MatchId;
        public Dictionary<string, string> Equality;
        public Dictionary<string, List<string>> Intersection;

        public MatchEnvironmentDTO(IDictionary env)
        {
            Tickets = new Dictionary<string, InjectedTicketDTO<A>>();

            foreach (DictionaryEntry entry in env)
            {
                string key = entry.Key.ToString();
                if (key == "MM_MATCH_PROFILE")
                {
                    MatchProfile = Env.TryParseEnvVariableString(entry);
                }
                else if (key == "MM_TICKET_IDS")
                {
                    TicketIds = Env.TryParseEnvVariableJSON<List<string>>(entry);
                }
                else if (key == "MM_GROUPS")
                {
                    Groups = Env.TryParseEnvVariableJSON<Dictionary<string, List<string>>>(entry);
                }
                else if (key == "MM_TEAMS")
                {
                    Teams = Env.TryParseEnvVariableJSON<Dictionary<string, List<string>>>(entry);
                }
                else if (key == "MM_MATCH_ID")
                {
                    MatchId = Env.TryParseEnvVariableString(entry);
                }
                else if (key == "MM_EQUALITY")
                {
                    Equality = Env.TryParseEnvVariableJSON<Dictionary<string, string>>(entry);
                }
                else if (key == "MM_INTERSECTION")
                {
                    Intersection = Env.TryParseEnvVariableJSON<Dictionary<string, List<string>>>(
                        entry
                    );
                }
                else if (key.StartsWith("MM_TICKET_"))
                {
                    InjectedTicketDTO<A> ticket = Env.TryParseEnvVariableJSON<InjectedTicketDTO<A>>(
                        entry
                    );
                    Tickets[ticket.ID] = ticket;
                }
            }
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
