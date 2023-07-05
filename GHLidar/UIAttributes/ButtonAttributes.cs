using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Collections.Generic;
using Rhino.Display;
using System.Drawing.Drawing2D;
using Rhino.Resources;
using System.Windows.Forms;

namespace SiteReader.UIAttributes
{
    public class ButtonAttributes : GH_ComponentAttributes
    {
        public ButtonAttributes(GH_Component owner) : base(owner) { }

        protected override void Layout()
        {
            base.Layout();

            System.Drawing.Rectangle rec0 = GH_Convert.ToRectangle(Bounds);
            rec0.Height += 22;

            System.Drawing.Rectangle rec1 = rec0;
            rec1.Y = rec1.Bottom - 22;
            rec1.Height = 22;
            rec1.Inflate(-2, -2);

            Bounds = rec0;
            ButtonBounds = rec1;
        }
        private System.Drawing.Rectangle ButtonBounds { get; set; }

        //has the las file been imported?


        //button appearance attributes
        private static readonly string ButtonSetText = "Clicked";
        private static readonly string ButtonUnsetText = "Click Me";
        private string ButtonText { get; set; } = ButtonUnsetText;

        protected override void Render(GH_Canvas canvas, System.Drawing.Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);

            if (channel == GH_CanvasChannel.Objects)
            {
                GH_Capsule button = GH_Capsule.CreateTextCapsule(ButtonBounds, ButtonBounds, GH_Palette.White, ButtonText, 2, 0);
                button.Render(graphics, Selected, Owner.Locked, false);
                button.Dispose();
            }
        }
        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left && Owner.RuntimeMessageLevel != GH_RuntimeMessageLevel.Warning && Owner.RuntimeMessageLevel != GH_RuntimeMessageLevel.Error)
            {
                System.Drawing.RectangleF rec = ButtonBounds;
                if (rec.Contains(e.CanvasLocation))
                {
                    Owner.RecordUndoEvent("Button Clicked");
                    ButtonText = ButtonText == ButtonUnsetText ? ButtonSetText : ButtonUnsetText;
                    Owner.OnDisplayExpired(true);
                    return GH_ObjectResponse.Handled;
                }
            }
            return base.RespondToMouseDown(sender, e);
        }
    }
}