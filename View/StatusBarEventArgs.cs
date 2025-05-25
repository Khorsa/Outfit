namespace OutfitTool.View
{
    class StatusBarEventArgs: EventArgs
    {
        public string Info;
        public StatusBarEventArgs(string Info) 
        { 
            this.Info = Info;
        }
    }
}
