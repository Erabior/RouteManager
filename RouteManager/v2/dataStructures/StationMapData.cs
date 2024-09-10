using RollingStock;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RouteManager.v2.dataStructures
{
    public class StationMapData
    {
        public Vector3 Pos0 { get; set; }
        public Vector3 Pos1 { get; set; }
        public Vector3 Center { get; set; }
        public float Length { get; set; } 
        public string Branch {  get; set; } //branch this station belongs to
        public List<string> Branches { get; set; } //branches connected to this station

        private bool HardEOL; //only one neighbour

        public PassengerStop station { get; set; }

        public bool EndOfLine {
            get 
            {
                if (HardEOL || station == null) {
                    return true;
                }

                //check how many neighbours are enabled; 1 enabled neighbour = EOL, >1 enabled neighbour = !EOL
                int availablestops = station.neighbors.Where(ps => !ps.ProgressionDisabled).Count();

                return availablestops <= 1;
                
            }
            private set { }
        }

        

        //Point coordinates of the referenced station in the 3d map space.
        public StationMapData(float x0, float y0, float z0, float x1, float y1, float z1, float xc, float yc, float zc, float len, bool endOfLine = false)
        {
            Pos0 = new Vector3(x0, y0, z0);
            Pos1 = new Vector3(x1, y1, z1);
            Center = new Vector3(xc, yc, zc);
            Length = len;
            HardEOL = endOfLine;
            Branches = new List<string>();
        }

    }
}
