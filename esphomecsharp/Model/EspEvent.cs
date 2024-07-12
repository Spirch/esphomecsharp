namespace esphomecsharp.Model;

public class EspEvent
{
    public string Id { get; set; }
    public object Value { get; set; }
    public string Name { get; set; }
    public string State { get; set; }
    public string Event_Type { get; set; }
}
