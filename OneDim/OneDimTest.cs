using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace OneDim
{
    [System.Runtime.InteropServices.Guid("e53b2411-4109-465e-b745-3ec7c95287f8")]
    public class OneDimTest : Command
    {
        public OneDimTest()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static OneDimTest Instance
        {
            get; private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "OneDimTest"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            //select the cross-section faces
            var gb = new Rhino.Input.Custom.GetObject();
            gb.SetCommandPrompt("Select the surfaces which form the cross-section of the beam.");
            gb.GeometryFilter = Rhino.DocObjects.ObjectType.Surface;
            gb.EnablePreSelect(false, true);
            gb.GetMultiple(1, 0);
            if (gb.CommandResult() != Rhino.Commands.Result.Success)
                return gb.CommandResult();

            List<BrepFace> cross_section = new List<BrepFace>();
            for (int i = 0; i < gb.ObjectCount; i++)
            {
                var face = gb.Object(i).Face();
                if (face != null)
                    cross_section.Add(face);
            }
            
            //select the rail
            Rhino.DocObjects.ObjRef rail_ref;
            var rc = RhinoGet.GetOneObject("Select rail curve", false, Rhino.DocObjects.ObjectType.Curve, out rail_ref);
            if (rc != Rhino.Commands.Result.Success)
                return rc;

            var rail_crv = rail_ref.Curve();
            if (rail_crv == null)
                return Rhino.Commands.Result.Failure;

            var gx = new Rhino.Input.Custom.GetObject();
            gx.SetCommandPrompt("Select cross section curves");
            gx.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
            gx.EnablePreSelect(false, true);
            gx.GetMultiple(1, 0);
            if (gx.CommandResult() != Rhino.Commands.Result.Success)
                return gx.CommandResult();

            var cross_sections = new List<Rhino.Geometry.Curve>();
            for (int i = 0; i < gx.ObjectCount; i++)
            {
                var crv = gx.Object(i).Curve();
                if (crv != null)
                    cross_sections.Add(crv);
            }
            if (cross_sections.Count < 1)
                return Rhino.Commands.Result.Failure;

            var sweep = new Rhino.Geometry.SweepOneRail();
            sweep.AngleToleranceRadians = doc.ModelAngleToleranceRadians;
            sweep.ClosedSweep = false;
            sweep.SweepTolerance = doc.ModelAbsoluteTolerance;
            //sweep.SetToRoadlikeTop();
            var breps = sweep.PerformSweep(rail_crv, cross_sections);
            for (int i = 0; i < breps.Length; i++)
                doc.Objects.AddBrep(breps[i]);
            doc.Views.Redraw();
            return Rhino.Commands.Result.Success;
        }
    }
}
