using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showdown_Replay_Scouter
{
    public class Pokemon
    {
        public string name = null;
        public string item = null;
        public string ability = null;
        public List<string> moves = new List<string>();

        public static string PrintPokemon(List<Pokemon> pokemon)
        {
            string s = "";
            foreach(Pokemon poke in pokemon)
            {
                s += poke.name + ((poke.item != null) ? " @ " + poke.item : "") + "\r\n";
                if (poke.ability != null) s += "Ability: " + poke.ability + "\r\n";
                foreach (string move in poke.moves)
                {
                    s += "- " + move + "\r\n";
                }
                s += "\r\n";
            }
            return s;
        }

        public Pokemon Clone()
        {
            return new Pokemon
            {
                name = this.name + "",
                item = this.item + "",
                ability = this.ability + "",
                moves = this.moves.Select(item => (string)item.Clone()).ToList()
            };
        }
    }
}
