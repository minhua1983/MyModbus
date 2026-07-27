namespace MyModbus.Common
{
    public class MyEventArgs
    {
        public object Body { get; set; }

        public MyEventArgs(object body) { 
            Body = body;
        }
    }
}
