using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Microsoft.Samples.Kinect.BodyBasics.Rectangle
{

    public class Square
        {
            Rect rect;
            TypeSquare orientation;
            int point = 2;
            Boolean isHit = false;
            Boolean isShown = true;
            int timeShown = 3;
            int counter = 0;

            public Square(Rect rect, TypeSquare orientation)
                {
                    this.rect = rect;
                    this.orientation = orientation;
        
                }

            public Rect Rect
                {
                    get => rect;
                }  

            public int Counter
        {
            get => counter;
        }

            public void DrawSquare(DrawingContext drawingContext, Brush brush)
                {
                    drawingContext.DrawRectangle(
                        brush,
                        null,
                        rect);
                }

        public void incCounter()
        {
            counter++;
        }

            public void ClearSquare(DrawingContext drawingContext)
                {
                    drawingContext.DrawRectangle(
                        Brushes.Black,
                        null,
                        rect);
                }



        }


    /*
    internal class Square
    {
        int x=20;
        int y=20;
        int a =5;
        TypeSquare orientation = TypeSquare.Up;
        int point = 2;
        Boolean isHit = false;


    }*/
}
