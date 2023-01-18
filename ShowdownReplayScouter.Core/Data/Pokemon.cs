using System.Collections.Generic;
using System.Linq;

namespace ShowdownReplayScouter.Core.Data
{
    public class Pokemon
    {
        private string? _name;
        public string? Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value?.Replace("-*", "");
            }
        }
        public string? FormName { get; set; }
        public ICollection<string> AltNames { get; set; } = new List<string>();

        public string? Ability { get; set; }
        public string? Item { get; set; }
        public string? TeraType { get; set; }
        public bool Lead { get; set; }
        public ICollection<string> Moves { get; set; } = new List<string>();

        public override string ToString()
        {
            return (Lead ? "(Lead) " : "") + (FormName ?? Name);
        }

        public Pokemon Clone()
        {
            return new Pokemon
            {
                Name = Name,
                FormName = FormName,
                Item = Item,
                Ability = Ability,
                Lead = Lead,
                Moves = Moves.Select(item => (string)item.Clone()).ToList(),
                TeraType = TeraType
            };
        }
    }
}
