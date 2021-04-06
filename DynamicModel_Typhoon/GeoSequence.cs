using System;
using System.Collections.Generic;

namespace DynamicModel_Typhoon
{
    internal class GeoSequence
    {
        private string name;
        private string id;
        private GeoSequence next = null;
        private GeoSequence before = null;
        private GeoProcess parentpro = null;
        private List<GeoState> states = null;
        private string description = "";
        
        private TimeSpan time;

        public string Name
        {
            set { name = value; }
            get { return name; }
        }
        public string ID
        {
            set { id = value; }
            get { return id; }
        }
        public GeoSequence Next
        {
            set { next = value; }
            get { return next; }
        }
        public GeoSequence Precede
        {
            set { before = value; }
            get { return before; }
        }
        public GeoProcess ParentProcess
        {
            set { parentpro = value; }
            get { return parentpro; }
        }
        public List<GeoState> ChildrenGeostates
        {
            set { states = value; }
            get { return states; }
        }

        public string Description
        {
            set { description = value; }
            get { return description; }
        }

        public TimeSpan TimeSpan
        {
            set { time = value; }
            get { return time; }
        }
    }
}