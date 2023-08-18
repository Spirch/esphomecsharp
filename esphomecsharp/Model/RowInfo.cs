using System;

namespace esphomecsharp.Model
{
    sealed public class RowInfo
    {
        public Server Server { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public int Padding { get; set; }
        public int Col { get; set; }
        public ConsoleColor Color { get; set; }

        public int? DbDescId { get; set; }

        public override string ToString()
        {
            return $"Name: {Name} Unit: {Unit} Col: {Col} DbDescId: {DbDescId}";
        }
    }
}
