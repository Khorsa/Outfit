namespace OutfitTool.View;

class StatusBarEventArgs: EventArgs
{
    public string Info;
    public StatusBarEventArgs(string info) 
    { 
        this.Info = info;
    }
}