namespace esphomecsharp.EF.Model
{
    public sealed class Error : IDbItem
    {
        public int? ErrorId { get; set; }
        public string Date { get; set; }
        public string DeviceName { get; set; }
        public string Message { get; set; }
        public bool IsHandled { get; set; }
    }
}
