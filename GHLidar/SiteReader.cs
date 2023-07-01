using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace SiteReader
{
    public class SiteReader : GH_AssemblyInfo
    {
        public override string Name => "Site Reader";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("157BA6F6-43D7-4AF1-A65E-E8279137C4CC");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}