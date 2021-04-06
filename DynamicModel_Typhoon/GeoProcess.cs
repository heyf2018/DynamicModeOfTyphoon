using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicModel_Typhoon
{
    class GeoProcess
    {
        private string name;
        private string id;
        private GeoProcess parent=null;
        private GeoProcess child=null;
        private List<GeoState> geostates= null;
        private List<GeoSequence> geosquences=null;
        private List<GeoProcess> subGeoprocess=null;
        private GeoEvent impartEvent = null;
        private GeoProcess pNext = null;
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
        public GeoProcess Parent
        {
            set { parent = value; }
            get { return parent; }
        }

        public GeoProcess Child
        {
            set { child = value; }
            get { return child; }
        }
        public GeoProcess Next
        {
            set { pNext = value; }
            get { return pNext; }
        }
        public List<GeoState> Geostates
        {
            set { geostates = value; }
            get { return geostates; }
        }
        public List<GeoSequence> GeoSquence
        {
            set { geosquences = value; }
            get { return geosquences; }
        }
        public List<GeoProcess>SubGeoprocess
        {
            set { subGeoprocess = value; }
            get { return subGeoprocess; }
        }

        public GeoEvent ImpactEvent
        {
            set { impartEvent = value; }
            get { return impartEvent; }
        }

        public string EndTime { get; internal set; }
        public GeoProcess Precede { get; internal set; }
        public string StartTime { get; internal set; }

        public GeoProcess()
        {            
        }
        public GeoProcess(string pname)
        {
            name = pname;
        }
        public GeoProcess(List<GeoState> pGeostates)
        {
            geostates = pGeostates;
        }
    }
}
