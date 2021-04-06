using System;
using System.Collections.Generic;


namespace DynamicModel_Typhoon
{
    internal class GeoState
    {
        private string name;
        private string id;
        private GeoState next = null;
        private GeoState before = null;
        private GeoProcess parentpro = null;
        private GeoSequence parentseq = null;

        private Dictionary<int,string> registerEventType;  //注册事件 以及出发条件  这个机制如何办
        //private List<IGeometry> geometry = null;
        private Dictionary<string,string> properties = null;
        private DateTime time;

        public string Name
        {
            set { name = value; }
            get { return name; }
        }
        public Dictionary<string, string> Properties
        {
            set { properties = value; }
            get { return properties; }
        }
        public string ID
        {
            set { id = value; }
            get { return id; }
        }
        public GeoState Next
        {
            set { next = value; }
            get { return next; }
        }
        public GeoState Precede
        {
            set { before = value; }
            get { return before; }
        }
        public GeoProcess ParentProcess
        {
            set { parentpro = value; }
            get { return parentpro; }
        }
        public GeoSequence ParentSequence
        {
            set { parentseq = value; }
            get { return parentseq; }
        }
        public Dictionary<int, string> RegisterEventType
        {
            set { registerEventType = value; }
            get { return registerEventType; }
        }
        //public List<IGeometry> StateGeometry
        //{
        //    set { geometry = value; }
        //    get { return geometry; }
        //}
        public DateTime StateTime
        {
            set { time = value; }
            get { return time; }
        }

        public object MutualState { get; internal set; }
    }
}