using ESRI.ArcGIS.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicModel_Typhoon
{
    public enum eventtype
    {
        Biological_pests_and_diseases,
        Debris_flow,
        Flood,
        Inwelling,
        Landslide,
        Levee_collapse,
        Marine_pullution,
        Mechanical_error,
        Rain_tide,
        Surge,
        Typhoon,
        Water_and_soil_loss,
        Waterlogging
    }
    public struct EventOutruptCondition
    {
        private int eventtype;
        private string condition;

    }
    class GeoEvent
    {
        private GeoProcess detialProcess = null;
        private List<GeoEvent> subEvent = null;
        private int eType;
        private string pName;
        private DateTime time;
        private Point pLocation;
        private string whereCondition;
        private List<GeoProcess> impactProcess = null;
        private Dictionary<string, string> properties;
        private string id;
        private GeoEvent next;
        private GeoEvent before;

        public string Name
        {
            set { pName = value; }
            get { return pName; }
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
        public GeoEvent Next
        {
            set { next = value; }
            get { return next; }
        }
        public GeoEvent Precede
        {
            set { before = value; }
            get { return before; }
        }
        public GeoProcess DetialProcess
        {
            set { detialProcess = value; }
            get { return detialProcess; }
        }
        public List<GeoEvent> SubEvent
        {
            set { subEvent = value; }
            get { return subEvent; }
        }
        public List<GeoProcess> ImpactProcess
        {
            set { impactProcess = value; }
            get { return impactProcess; }
        }
        public int EventType
        {
            set { eType = value; }
            get { return eType; }
        }
        public string WhereCondition
        {
            set { whereCondition = value; }
            get { return whereCondition; }
        }
        public DateTime StateTime
        {
            set { time = value; }
            get { return time; }
        }
        public Point Location
        {
            set { pLocation = value; }
            get { return pLocation; }
        }
    }


}
