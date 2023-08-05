namespace esphomecsharp.EF.Model
{
    sealed public class Event : IDbItem
    {
        public long EventId { get; set; }

        public int RowEntryId { get; set; }
        public string Date { get; set; }
        public string ValueString
        {
            get
            {
                if (double.TryParse(Value.ToString(), out double value))
                    return value.ToString(GlobalVariable.RES_DOUBLE_STRING);

                return Value.ToString();
            }
            set
            {
                Value = value;
            }
        }

        public string Id { get; set; }
        public dynamic Value { get; set; }
        public string Name { get; set; }
        public string State { get; set; }

        public RowEntry EspHomeId { get; set; }
    }
}
