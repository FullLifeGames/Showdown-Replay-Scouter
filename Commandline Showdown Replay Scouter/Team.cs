using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Showdown_Replay_Scouter
{
    public class Team
    {

        public int id;
        public string[] pokemon = new string[6];

        public int Length()
        {
            for (int i = 0; i < pokemon.Length; i++)
            {
                if (pokemon[i] == null)
                {
                    return i;
                }
            }
            return pokemon.Length;
        }

        public int Compare(List<Team> referenz)
        {
            if (pokemon[0] == null)
            {
                
            }
            else
            {
                pokemon = pokemon.Select(poke => (poke != null && poke.Trim() != "") ? poke.Trim() : "ZZ").ToArray();
                Array.Sort(pokemon);
                pokemon = pokemon.Select(poke => (poke == "ZZ") ? "" : poke).ToArray();
                foreach (Team t in referenz)
                {
                    if (pokemon.SequenceEqual(t.pokemon))
                    {
                        return t.id;
                    }
                }
            }
            return 0;
        }

        internal string Compare(Dictionary<string, List<Team>> saveRef)
        {
            if (pokemon[0] == null)
            {

            }
            else
            {
                pokemon = pokemon.Select(poke => (poke != null && poke.Trim() != "") ? poke.Trim() : "ZZ").ToArray();
                Array.Sort(pokemon);
                pokemon = pokemon.Select(poke => (poke == "ZZ") ? "" : poke).ToArray();
                foreach (KeyValuePair<string, List<Team>> kv in saveRef)
                {
                    foreach (Team t in kv.Value)
                    {
                        if (pokemon.SequenceEqual(t.pokemon))
                        {
                            return kv.Key;
                        }
                    }
                }
            }
            return "0";
        }
    }
}
