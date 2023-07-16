using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Printing;
using System.IO;
using Newtonsoft.Json;
using SiteReader.PDAL;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace SiteReader
{
    public class ImportLas : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>

        public ImportLas()
          : base("Reference LAS", "refLAS",
            "Reference a LAS file before filtering. Display LAS file properties.",
            "Site Reader", "LAS")
        {
        }

        //FIELDS
        private float Value;

        //properties
        public void SetVal(float value)
        {
            Value = value;
        }

        //This region overrides the typical component layout
        public override void CreateAttributes()
        {
            m_attributes = new UIAttributes.BaseAttributes(this, SetVal);
        }

        //global variables
        private bool iVal = false;
        public bool ImpValue
        {
            get { return iVal; }
            set { iVal = value; }
        }


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // Use the pManager object to register your input parameters.
            // You can often supply default values when creating parameters.
            // All parameters must have the correct access type. If you want 
            // to import lists or trees of values, modify the ParamAccess flag.
            pManager.AddTextParameter("file path", "path", "Path to LAS or LAZ file.", GH_ParamAccess.item);

            // If you want to change properties of certain parameters, 
            // you can use the pManager instance to access them by index:
            //pManager[0].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Use the pManager object to register your output parameters.
            // Output parameters do not have default values, but they too must have the correct access type.
            pManager.AddTextParameter("Output", "out", "Component messages. Use to check for errors.", GH_ParamAccess.item);
            pManager.AddTextParameter("LAS Header", "header", "Useful information about the LAS file", GH_ParamAccess.list);
            pManager.AddGenericParameter("test", "test", "test", GH_ParamAccess.item);

            // Sometimes you want to hide a specific parameter from the Rhino preview.
            // You can use the HideParameter() method as a quick way:
            //pManager.HideParameter(0);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Input variables
            string testPath = String.Empty;

            //output variables
            string outMsg = "";

            // Is input empty?
            if (!DA.GetData(0, ref testPath)) return;

            // Test if file exists
            if (!File.Exists(testPath))
            {
                outMsg = "Cannot find file.";
                DA.SetData(0, outMsg);
                return;
            }

            // Test if file is .las or .laz
            if (!GetFileExt(testPath))
            {
                outMsg = "Invalid file type";
                DA.SetData(0, outMsg);
                return;
            }

            iVal = true;
            List<object> pipe = new List<object>();
            pipe.Add(testPath);
            string json = JsonConvert.SerializeObject(pipe.ToArray());

            Pipeline pl = new Pipeline(json);

            long count = pl.Execute();
            string header = pl.Metadata;

            (List<string> uiList, List<float> ptShifts, string epsgCode) = GetHeaderInfo(header);

            //output 
            DA.SetData(0, Value.ToString());
            DA.SetDataList(1, uiList);
            DA.SetData(2, pl);

        }

        bool GetFileExt(string path)
        {
            string fileExt = System.IO.Path.GetExtension(path);

            if (fileExt == ".las" || fileExt == ".laz")
            {
                return true;
            }
            return false;
        }

        
        (List<string>, List<float>, string) GetHeaderInfo(string header)
        {
            //the header values to include
            List<string> headerList = new List<string> { "minx", "miny", "minz", "maxx", "maxy", "maxz", "count"};

            //formatted values to display
            List<string> displayList = new List<string> { "Projection: ","Min. X Value: ", "Min. Y Value: ", "Min. Z Value: ", 
                                                                      "Max. X Value: ", "Max. Y Value: ", "Max. Z Value: ", "Point Count: " };
            //list of point cloud shifts
            List<float> shiftList = new List<float> { 0,0,0,0,0,0 };

            //project value
            string projection = null;

            string innerJson = JObject.Parse(header)["metadata"]["readers.las"][0].ToString();
            JObject jObj = JObject.Parse(innerJson);

            foreach (var pair in jObj)
            {
                if (pair.Key == "comp_spatialreference")
                {
                    string epsgCode = GetProjection(pair.Value.ToString());

                    if (epsgCode != null)
                    {
                        displayList[0] += $"EPSG:{epsgCode}";
                        projection = epsgCode;
                    }
                    else
                    {
                        displayList[0] += "Not provided in file. Check file source or website.";
                    }
                } 
                else if (headerList.Contains(pair.Key))
                {
                    int index = headerList.IndexOf(pair.Key);

                    displayList[index + 1] += pair.Value.ToString();

                    if (pair.Key != "count")
                    {
                        shiftList[index] = pair.Value.Value<float>();
                    }
                }
            }

            return (displayList, shiftList, projection);
        }

        string GetProjection(string wkt)
        {
            string pattern = @"AUTHORITY\[""\w+"",(\d+)\]";
            Match match = Regex.Match(wkt, pattern);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return null;
        }
        


        /// <summary>
        /// The Exposure property controls where in the panel a component icon 
        /// will appear. There are seven possible locations (primary to septenary), 
        /// each of which can be combined with the GH_Exposure.obscure flag, which 
        /// ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => null;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("ACA692CB-5F20-4B17-966E-BF7D46790B11");
    }
}