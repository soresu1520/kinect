//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.BodyBasics
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using static System.Console;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using Microsoft.Samples.Kinect.BodyBasics.Rectangle;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {


        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 30;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        private List<Pen> bodyColors;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        private int counter=0;

        private List<Square> squares = new List<Square>();

        private Random r = new Random();

        private int squareX, squareY;

        private int squareSize = 50;

        private float positionRightHandY, positionLeftHandY;

        private Boolean aboveFlag = false;
        private Boolean inFlag = false;
        private Boolean belowFlag = false;
        private Boolean leftFlag = false;
        private Boolean rightFlag = false;

        private int timerCount = 1000;
        private int points = 0;

        private int distanceFormScreen = 50;
        private int addDistance = 10;


        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();

            this.squareX = r.Next(this.distanceFormScreen, this.displayWidth-this.squareSize-this.distanceFormScreen);
            this.squareY = r.Next(this.distanceFormScreen, this.displayHeight-this.squareSize-this.distanceFormScreen);

            Square square = new Square(this.squareX, this.squareY, this.squareSize);
            //Square square = new Square(new Rect(300, 100, this.squareSize, this.squareSize), TypeSquare.Down);
            //Square square2 = new Square(new Rect(r.Next(0, this.displayWidth), r.Next(0, this.displayHeight), 50, 50), TypeSquare.Up);

            this.squares.Add(square);
            //this.squares.Add(square2);


        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute start up tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            //Trace.WriteLine("WHATEVER");
            bool dataReceived = false;

            
            if (this.counter == 200)
            {
                this.squares.Remove(this.squares[0]);
                this.counter = 0;
                this.squareX = r.Next(this.distanceFormScreen, this.displayWidth-this.squareSize-this.distanceFormScreen);
                this.squareY = r.Next(this.distanceFormScreen, this.displayHeight-this.squareSize-this.distanceFormScreen);
                this.squares.Add(new Square(this.squareX, this.squareY, this.squareSize));
            }



            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {


                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));




                    foreach(Square sqr in squares)
                    {
                        DrawSquare(dc, sqr);
                    }


                    int penIndex = 0;
                    foreach (Body body in this.bodies)
                    {
                        Pen drawPen = this.bodyColors[penIndex++];

                        if (body.IsTracked)
                        {
                            this.DrawClippedEdges(body, dc);

                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            foreach (JointType jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                CameraSpacePoint position = joints[jointType].Position;
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            }

                            this.DrawBody(joints, jointPoints, dc, drawPen);

                            this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);

                            //Trace.WriteLine(this.squares[0].typeSquare);

                            //KINDA WORKS

                            //POZYCJE
                            double xHandRight = jointPoints[JointType.WristRight].X;
                            double yHandRight = jointPoints[JointType.WristRight].Y;
                            double xHandLeft = jointPoints[JointType.WristLeft].X;
                            double yHandLeft = jointPoints[JointType.WristLeft].Y;

                            dc.DrawEllipse(this.handOpenBrush, null, jointPoints[JointType.WristLeft], 15, 15);
                            dc.DrawEllipse(this.handOpenBrush, null, jointPoints[JointType.WristRight], 15, 15);

                           /* dc.DrawRectangle(Brushes.Green, null, new Rect(xHandRight, 
                                yHandRight, 20, 20));

                            dc.DrawRectangle(Brushes.Green, null, new Rect(xHandLeft, 
                                yHandLeft, 20, 20));*/

                            //DO USUNI??CIA POTEM
                            //this.squareX = 300;
                            //this.squareY=150;

                            //BOOLEANS
                            //right hand in square
                            Boolean rightHandPositionX = xHandRight >= this.squareX && xHandRight <= (this.squareX + this.squareSize);
                            //Boolean rightHandPositionY = yHandRight <= this.squareY && yHandRight >= (this.squareY - this.squareSize);
                            Boolean rightHandPositionY = yHandRight >= this.squareY && yHandRight <= (this.squareY + this.squareSize);
                            Boolean leftHandPositionX = xHandLeft >= this.squareX && xHandLeft <= (this.squareX + this.squareSize);
                            Boolean leftHandPositionY = yHandLeft >= this.squareY && yHandLeft <= (this.squareY + this.squareSize);

                            //right hand above square
                            Boolean rightHandAboveY = yHandRight <= (this.squareY - this.squareSize);
                            Boolean leftHandAboveY = yHandLeft <= (this.squareY - this.squareSize);
                            //right hand below square
                            Boolean rightHandBelowY = yHandRight >= this.squareY;
                            Boolean leftHandBelowY = yHandLeft >= this.squareY;

                            //hand on the left of the square
                            Boolean rightHandLeftX = xHandRight <= this.squareX;
                            Boolean leftHandLeftX = xHandLeft <= this.squareX;
                            Boolean rightHandLeftY = yHandRight >= this.squareY && yHandRight <= this.squareY + this.squareSize;
                            Boolean leftHandLeftY = yHandLeft >= this.squareY && yHandLeft <= this.squareY + this.squareSize;

                            //hand on the right of the square
                            Boolean rightHandRightX = xHandRight >= this.squareX + this.squareSize;
                            Boolean leftHandRightX = xHandLeft >= this.squareX + this.squareSize;
                            Boolean rightHandRightY = rightHandPositionY;
                            Boolean leftHandRightY = leftHandPositionY;

                            Boolean if1 = xHandRight >= this.squareX && xHandRight <= this.squareX + this.squareSize;
                            Boolean if2 = yHandRight <= this.squareY;
                            Boolean if3 = xHandLeft >= this.squareX && xHandLeft <= this.squareX + this.squareSize;
                            Boolean if4 = yHandLeft <= this.squareY;
                            Boolean if5 = yHandRight >= this.squareY && yHandRight <= (this.squareY + this.squareSize);
                            Boolean if6 = yHandRight >= this.squareY + this.squareSize;
                            Boolean if7 = yHandLeft >= this.squareY + this.squareSize;

                            //Trace.WriteLine("Hand: " + " : " + yHandRight + " ; Square: " + this.squareY + " If: " + rightHandPositionY);

                            //WARUNKI
                            //if(rightHandPositionX && rightHandPositionY)
                           // {
                             //   Trace.WriteLine("HAND IN SQUARE");
                            //}

                            //DOWN (reka idzie z gory na dol) 
                            if (this.squares[0].Orientation == TypeSquare.Down)
                            {

                                if((if2 && if1) || (if3 && if4))
                                {
                                    Trace.WriteLine("Reka nad kwadratem DOWN");
                                    this.aboveFlag = true;
                                }

                                if ((this.aboveFlag && rightHandPositionX && if5) || (this.aboveFlag && leftHandPositionX && leftHandPositionY))
                                 {
                                    Trace.WriteLine("R??ka w kwadracie DOWN ");
                                    this.inFlag = true;
                                 }

                                if(this.inFlag && ((if1 && if6) || (if3 && if7)))
                                {
                                    Trace.WriteLine("Reka pod kwadratem DOWN");
                                    this.belowFlag = true;
                                }

                                if( this.aboveFlag && this.inFlag && this.belowFlag)
                                {
                                    Trace.WriteLine("Zbity kwadrat DOWN");
                                    this.aboveFlag = false;
                                    this.inFlag = false;
                                    this.belowFlag = false;
                                    
                                    this.squares.Remove(this.squares[0]);
                                    this.counter = 0;
                                    this.points = this.points + 1;
                                    this.squareX = r.Next(this.distanceFormScreen, this.displayWidth-this.squareSize-this.distanceFormScreen);
                                    this.squareY = r.Next(this.distanceFormScreen, this.displayHeight-this.squareSize-this.distanceFormScreen);
                                    this.squares.Add(new Square(this.squareX, this.squareY, this.squareSize));
                                }

                                if(!rightHandPositionX && !leftHandPositionX)
                                {
                                    this.aboveFlag = false;
                                    this.inFlag = false;
                                    this.belowFlag = false;
                                }

                            }
                            Trace.WriteLine(this.squares[0].Orientation);

                            //UP 
                            if (this.squares[0].Orientation == TypeSquare.Up)
                            {
                           

                                if((if1 && if6) || (if3 && if7))
                                {
                                    Trace.WriteLine("Reka pod kwadratem UP");
                                    this.belowFlag = true;
                                }

                                if ((this.belowFlag && rightHandPositionX && rightHandPositionY) || (this.belowFlag && leftHandPositionX && leftHandPositionY))
                                 {
                                    Trace.WriteLine("R??ka w kwadracie UP ");
                                    this.inFlag = true;
                                 }

                                if(this.inFlag && ((if2 && if1) || (if3 && if4)))
                                {
                                    Trace.WriteLine("Reka nad kwadratem UP");
                                    this.aboveFlag = true;
                                }

                                if( this.aboveFlag && this.inFlag && this.belowFlag)
                                {
                                    Trace.WriteLine("Zbity kwadrat UP");
                                    this.aboveFlag = false;
                                    this.inFlag = false;
                                    this.belowFlag = false;
                                    
                                    this.squares.Remove(this.squares[0]);
                                    this.counter = 0;
                                    this.points = this.points + 1;
                                    this.squareX = r.Next(this.distanceFormScreen, this.displayWidth-this.squareSize-this.distanceFormScreen);
                                    this.squareY = r.Next(this.distanceFormScreen, this.displayHeight-this.squareSize-this.distanceFormScreen);
                                    this.squares.Add(new Square(this.squareX, this.squareY, this.squareSize));
                                }

                                if(!rightHandPositionX && !leftHandPositionX)
                                {
                                    this.aboveFlag = false;
                                    this.inFlag = false;
                                    this.belowFlag = false;
                                }
                                
                               
                            }



                            //RIGHT
                            //r??ka idzie z lewej do prawej (z naszej perspektywy)
                            if (this.squares[0].Orientation == TypeSquare.Right)
                            {
                                //Trace.WriteLine(rightHandPositionY);

                                if(( rightHandLeftX && rightHandLeftY) || ( leftHandLeftX && leftHandLeftY))
                                {
                                    Trace.WriteLine("R??ka na lewo od kwadratu RIGHT");
                                    this.leftFlag = true;
                                }
                                
                                if(this.leftFlag && ((rightHandPositionX && rightHandLeftY) || (leftHandPositionX && leftHandLeftY)))
                                {
                                    Trace.WriteLine("R??ka w kwadracie RIGHT");
                                    this.inFlag = true;
                                }
                                
                                
                                if(this.inFlag && ((rightHandRightX && rightHandLeftY) || (leftHandRightX  && leftHandLeftY)))
                                {
                                    Trace.WriteLine("R??ka po prawo od kwadratu RIGHT");
                                    this.rightFlag = true;
                                }
                                
                                
                                if(this.leftFlag && this.inFlag && this.rightFlag)
                                {
                                    Trace.WriteLine("Zbity kwadrat RIGHT");
                                    this.leftFlag = false;
                                    this.inFlag = false;
                                    this.rightFlag = false;

                                    this.squares.Remove(this.squares[0]);
                                    this.counter = 0;
                                    this.points = this.points + 1;
                                    //to zmieni?? jak tam wy??ej to zadzia??a
                                    this.squareX = r.Next(0, this.displayWidth - this.squareSize - this.distanceFormScreen);
                                    this.squareY = r.Next(0, this.displayHeight - this.squareSize - this.distanceFormScreen);
                                    this.squares.Add(new Square(this.squareX, this.squareY, this.squareSize));
                                }

                                //nie wiem co to ma tutaj robi??
                                if (!rightHandPositionX && !leftHandPositionX)
                                {
                                    this.aboveFlag = false;
                                    this.inFlag = false;
                                    this.belowFlag = false;
                                }

                            }


                            //LEFT
                            //r??ka idzie z prawej do lewej (z naszej perspektywy)
                            if (this.squares[0].Orientation == TypeSquare.Left)
                            {
                                //Trace.WriteLine(rightHandPositionY);

                                if((rightHandRightX && rightHandLeftY) || (leftHandRightX  && leftHandLeftY))
                                {
                                    Trace.WriteLine("R??ka na lewo od kwadratu LEFT");
                                    this.rightFlag = true;
                                }
                                
                                if(this.rightFlag && ((rightHandPositionX && rightHandLeftY) || (leftHandPositionX && leftHandLeftY)))
                                {
                                    Trace.WriteLine("R??ka w kwadracie LEFT");
                                    this.inFlag = true;
                                }
                                
                                
                                if(this.inFlag && (( rightHandLeftX && rightHandLeftY) || ( leftHandLeftX && leftHandLeftY)))
                                {
                                    Trace.WriteLine("R??ka po prawo od kwadratu LEFT");
                                    this.leftFlag = true;
                                }
                                
                                
                                if(this.leftFlag && this.inFlag && this.rightFlag)
                                {
                                    Trace.WriteLine("Zbity kwadrat LEFT");
                                    this.leftFlag = false;
                                    this.inFlag = false;
                                    this.rightFlag = false;

                                    this.squares.Remove(this.squares[0]);
                                    this.counter = 0;
                                    this.points = this.points + 1;
                                    //to zmieni?? jak tam wy??ej to zadzia??a
                                    this.squareX = r.Next(0, this.displayWidth - this.squareSize - this.distanceFormScreen);
                                    this.squareY = r.Next(0, this.displayHeight - this.squareSize - this.distanceFormScreen);
                                    this.squares.Add(new Square(this.squareX, this.squareY, this.squareSize));
                                }

                                //nie wiem co to ma tutaj robi??
                                if (!rightHandPositionX && !leftHandPositionX)
                                {
                                    this.aboveFlag = false;
                                    this.inFlag = false;
                                    this.belowFlag = false;
                                }

                            }



                        }

                     
                    }

                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));


                    //zbijanie kwadrat??w nie dzia??a lol
                    //if(this.counter != 0 && (positionRightHandY==this.displayHeight/2 || positionLeftHandY==this.displayHeight/2))
                    //{
                        //dc.DrawRectangle(Brushes.Pink, null, new Rect(20, 20, 20, 20));
                    //}

                    //check position


                }
                this.counter = this.counter+1;
                myBlockText.Text = "Punkty: " + this.points;


               
            }
        }


        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            /*
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }*/
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping body data
        /// </summary>
        /// <param name="body">body to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }
        
        private void DrawSquare(DrawingContext drawingContext, Square square)
        {
            square.DrawSquare(drawingContext);

        } 
    }
}
