﻿namespace esphomecsharp.Model
{
    sealed public class RowInfo
    {
        public string Name { get; set; }
        public string Unit { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }

        public int? DbDescId { get; set; }
    }
}