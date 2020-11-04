using Fusee.Base.Common;
using Fusee.Base.Core;
using Fusee.Engine.Common;
using Fusee.Engine.Core;
using Fusee.Engine.Core.Scene;
using Fusee.Math.Core;
using Fusee.Serialization;
using Fusee.Xene;
using static Fusee.Engine.Core.Input;
using static Fusee.Engine.Core.Time;
using Fusee.Engine.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using Fusee.Xirkit;
using System.Text;

namespace FuseeApp
{
    [FuseeApplication(Name = "Starship", Description = "Yet another FUSEE App.")]
    public class Starship : RenderCanvas
    {

        private SceneContainer _starshipScene;
        private SceneRendererForward _sceneRenderer;


        private Transform _starshipTrans;
        private Transform _trenchTrans;

        private Mesh _starShipMesh;


   
        private float appStartTime;             //Die Zeit seit Start der Applikation
        private float playTime;                 //Die Zeit seit drücken des Startknopfs (Leertaste)             wird später benutzt für das Leaderboard/aktive Anzeige ingame(?)
        private bool start;


        bool left;
        bool mid = true;
        bool right;

        bool up;
        bool normal = true;
        bool down;



        //Kollisionsvariablen 
        private AABBf _shipBox; 

        private Transform _cubeObstTrans;
      
        private Mesh _cubeObstMesh;
      





        public override void Init()
        {
            RC.ClearColor = new float4(0, 0, 0, 0);

            _starshipScene = AssetStorage.Get<SceneContainer>("StarshipProtoSeparateTrenches.fus");

            //Trench0; 1, 2, 3

            _sceneRenderer = new SceneRendererForward(_starshipScene);

        }



        public override void RenderAFrame()
        {
            // Clear the backbuffer
           RC.Clear(ClearFlags.Color | ClearFlags.Depth);

            RC.Viewport(0, 0, Width, Height);


    
            _starshipTrans = _starshipScene.Children.FindNodes(node => node.Name == "Ship")?.FirstOrDefault()?.GetTransform();

            _starShipMesh = _starshipScene.Children.FindNodes(node => node.Name == "Ship")?.FirstOrDefault()?.GetMesh();

            //_trenchTrans = _starshipScene.Children.FindNodes(node => node.Name == "Trench1")?.FirstOrDefault()?.GetTransform();





            double speed = (double)DeltaTime * 20;



            //Bounding Boxes 

            //Boundind Box des Schiffs
            _shipBox = _starShipMesh.BoundingBox;
          


            //Bounding Box des Cube Obstacles
            //_obstBox = _cubeObstMesh.BoundingBox;



            //Hier werden für Trenches sowie ihre jeweiligen obstacles Listen erstellt, die einzeln abgegangen werden, um nach einer Kollision zu prüfen
            List<SceneNode> TrenchList = _starshipScene.Children.FindNodes(node => node.Name.Contains("Trench")).ToList();
            TrenchList.RemoveAt(1);
            TrenchList.RemoveAt(1);

            if (TrenchList.Count() > 1)
            {
                TrenchList.RemoveAt(2);
                TrenchList.RemoveAt(2);
            }



            //System.Console.WriteLine(TrenchList.Count);

            for (int j = 0; j < TrenchList.Count(); j++)
            { 
                _trenchTrans = TrenchList.ElementAt(j).GetTransform();
                //System.Console.WriteLine(_trenchTrans.Translation);

                //System.Console.WriteLine(_trenchTrans);

                if (_trenchTrans != null)
                {
                    List<SceneNode> ObstaclesList = TrenchList.ElementAt(j).Children.FindNodes(node => node.Name.Contains("ObstacleDummy")).ToList();

                    for (int i = 0; i < ObstaclesList.Count(); i++)
                    {
                        _cubeObstTrans = _starshipScene.Children.FindNodes(node => node.Name == "ObstacleDummy" + i)?.FirstOrDefault()?.GetTransform();

                        _cubeObstMesh = _starshipScene.Children.FindNodes(node => node.Name == "ObstacleDummy" + i)?.FirstOrDefault()?.GetMesh();

                        AABBf cubeHitbox = _trenchTrans.Matrix() * _cubeObstTrans.Matrix() * _cubeObstMesh.BoundingBox;

                        Trench(_shipBox, cubeHitbox, start, speed, playTime);
                    }
                }
                
            }


            //System.Console.WriteLine(start);



            //Steuerung links/rechts
            if (Keyboard.IsKeyDown(KeyCodes.Left))
            {
                if (left)
                {
                    //do nothing
                    mid = false;
                }
                else if (mid)
                {
                    _starshipTrans.Translation.x -= 3;
                    mid = false;
                    left = true;
                    
                }
                else if (right)
                {
                    _starshipTrans.Translation.x -= 3;
                    right = false;
                    mid = true;
                }
                _starshipTrans.Rotation.x = Keyboard.LeftRightAxis * 0.167f;
            }
            if (Keyboard.IsKeyDown(KeyCodes.Right))
            {
                if (left)
                {
                    _starshipTrans.Translation.x += 3;
                    left = false;
                    mid = true;
                }
                else if (mid)
                {
                    _starshipTrans.Translation.x += 3;
                    mid = false;
                    right = true;
                }
                else if (right)
                {
                    //do nothing
                    mid = false;
                }
                _starshipTrans.Rotation.x = Keyboard.LeftRightAxis * 0.167f;
            }

            //oben / unten
            if (Keyboard.IsKeyDown(KeyCodes.Up))
            {
                if (up)
                {
                    //do nothing
                    normal = false;
                }
                else if (normal)
                {
                    _starshipTrans.Translation.y += 2.5f;
                    normal = false;
                    up = true;

                }
                else if (down)
                {
                    _starshipTrans.Translation.y += 2.5f;
                    down = false;
                    normal = true;
                }
                _starshipTrans.Rotation.z = + 0.083f;
            }
            if (Keyboard.IsKeyDown(KeyCodes.Down))
            {
                if (up)
                {
                    _starshipTrans.Translation.y -= 2.5f;
                    up = false;
                    normal = true;
                }
                else if (normal)
                {
                    _starshipTrans.Translation.y -= 2.5f;
                    normal = false;
                    down = true;
                }
                else if (down)
                {
                    //do nothing
                    normal = false;
                }
                _starshipTrans.Rotation.z = -0.083f;
            }






            //flüssige Steuerung

            //if (Keyboard.LeftRightAxis != 0)
            //{               
            //    if (Keyboard.LeftRightAxis > 0) //rechts
            //    {
            //         _starshipTrans.Translation.x += 10 * DeltaTime * Keyboard.LeftRightAxis;

            //        //leichter tilt nach links
            //        _starshipTrans.Rotation.x = Keyboard.LeftRightAxis * 0.167f;


            //        //Bewegungsgrenze rechts
            //        if (_starshipTrans.Translation.x >= 3.8f)
            //        {
            //            _starshipTrans.Translation.x -= 10 * DeltaTime * Keyboard.LeftRightAxis;
            //        }

            //    }
            //    else if (Keyboard.LeftRightAxis < 0)  //links
            //    {
            //        _starshipTrans.Translation.x += 10 * DeltaTime * Keyboard.LeftRightAxis;

            //        //leichter tilt nach rechts
            //        _starshipTrans.Rotation.x = Keyboard.LeftRightAxis * 0.167f;


            //        //Bewegungsgrenze links
            //        if (_starshipTrans.Translation.x <= -3.8f)
            //        {
            //            _starshipTrans.Translation.x -= 10 * DeltaTime * Keyboard.LeftRightAxis;
            //        }
            //    }
            //    //Console.WriteLine(_starshipTrans.Translation.x);
            //}

            //Steuerung oben/unten

            //if (Keyboard.UpDownAxis != 0)
            //{
            //    if (Keyboard.UpDownAxis > 0) //oben
            //    {
            //        _starshipTrans.Translation.y += 7 * DeltaTime * Keyboard.UpDownAxis;

            //        //leichter tilt nach oben
            //        _starshipTrans.Rotation.z = -Keyboard.UpDownAxis * 0.083f;


            //        //Bewegungsgrenze oben
            //        if (_starshipTrans.Translation.y >= 4.5f)
            //        {
            //            _starshipTrans.Translation.y -= 7 * DeltaTime * Keyboard.UpDownAxis;
            //        }
            //    }
            //    else if (Keyboard.UpDownAxis < 0) //unten
            //    {
            //        _starshipTrans.Translation.y += 7 * DeltaTime * Keyboard.UpDownAxis;

            //        //leichter tilt nach unten
            //        _starshipTrans.Rotation.z = -Keyboard.UpDownAxis * 0.083f;


            //        //Bewegungsgrenze unten
            //        if (_starshipTrans.Translation.y <= -0.3f)
            //        {
            //        _starshipTrans.Translation.y -= 7 * DeltaTime * Keyboard.UpDownAxis;
            //        }
            //    }
            //}




            //Start
            //Hier Startbutton / Menü einfügen


           

            if (Keyboard.IsKeyDown(KeyCodes.Space))  //wenn die Leertaste gedrückt wird, wird die Zeit seit dem Drücken in playTime gespeichert
            {
                start = true;
                appStartTime = RealTimeSinceStart;
                var irgendwas = AssetStorage.Get<SceneContainer>("Trench 1.fus").ToSceneNode();
                    _starshipScene.Children.Add(irgendwas);
            }


            if(start)
            { 
                //speed *= playTime;            unnötig, aber in ähnlicher Form später für die Geschwindigkeitserhöhung nutzbar
                _trenchTrans.Translation.z -= (float)speed;         //Die Bewegung der Szene wird aktiviert
                playTime = StartTimer(appStartTime);
                
            }
            else
            {
                playTime = 0;  //???? idk ob das so was bringt, aber ist erstmal egal
                speed = 0;
            }
      



            RC.View =  float4x4.LookAt(0, 3, -8, 0, 2, 0, 0, 1, 0);   


            //Tick any animations and Render the scene loaded in Init()
            _sceneRenderer.Render(RC);

            // Swap buffers: Show the contents of the backbuffer (containing the currently rendered frame) on the front buffer.
            Present();

        }





        //Start des Spiels, also Beginn der Bewegung der Szene und des Zählers
    

        //Timer wird gestartet
        private float StartTimer(float appStartTime)
        {

            return RealTimeSinceStart - appStartTime;    
            
        }
    
        
        
        //Trench Class
        private void Trench(AABBf shipHitbox, AABBf cubeHitbox,bool start, double speed, float playTime)
        {
   
            if (shipHitbox.Intersects(cubeHitbox))
            {
                Console.WriteLine("Boom!");
                Death();
            }  
        }




        private void Death()
        {
            start = false;
        }









































        //// Horizontal and vertical rotation Angles for the displayed object 
        //private static float _angleHorz = M.PiOver4, _angleVert;

        //// Horizontal and vertical angular speed
        //private static float _angleVelHorz, _angleVelVert;

        //// Overall speed factor. Change this to adjust how fast the rotation reacts to input
        //private const float RotationSpeed = 7;

        //// Damping factor 
        //private const float Damping = 0.8f;

        //private SceneContainer _rocketScene;
        //private SceneRendererForward _sceneRenderer;

        //private bool _keys;

        //// Init is called on startup. 
        //public override void Init()
        //{
        //    // Set the clear color for the backbuffer to white (100% intensity in all color channels R, G, B, A).
        //    RC.ClearColor = new float4(0, 0, 0, 0);

        //    // Load the rocket model
        //    _rocketScene = AssetStorage.Get<SceneContainer>("StarshipProtoSmall.fus");

        //    // Wrap a SceneRenderer around the model.
        //    _sceneRenderer = new SceneRendererForward(_rocketScene);
        //}

        //// RenderAFrame is called once a frame
        //public override void RenderAFrame()
        //{
        //    // Clear the backbuffer
        //    RC.Clear(ClearFlags.Color | ClearFlags.Depth);

        //    RC.Viewport(0, 0, Width, Height);

        //    // Mouse and keyboard movement
        //    if (Keyboard.LeftRightAxis != 0 || Keyboard.UpDownAxis != 0)
        //    {
        //        _keys = true;
        //    }

        //    if (Mouse.LeftButton)
        //    {
        //        _keys = false;
        //        _angleVelHorz = -RotationSpeed * Mouse.XVel * DeltaTime * 0.0005f;
        //        _angleVelVert = -RotationSpeed * Mouse.YVel * DeltaTime * 0.0005f;
        //    }
        //    else if (Touch.GetTouchActive(TouchPoints.Touchpoint_0))
        //    {
        //        _keys = false;
        //        var touchVel = Touch.GetVelocity(TouchPoints.Touchpoint_0);
        //        _angleVelHorz = -RotationSpeed * touchVel.x * DeltaTime * 0.0005f;
        //        _angleVelVert = -RotationSpeed * touchVel.y * DeltaTime * 0.0005f;
        //    }
        //    else
        //    {
        //        if (_keys)
        //        {
        //            _angleVelHorz = -RotationSpeed * Keyboard.LeftRightAxis * DeltaTime;
        //            _angleVelVert = -RotationSpeed * Keyboard.UpDownAxis * DeltaTime;
        //        }
        //        else
        //        {
        //            var curDamp = (float)System.Math.Exp(-Damping * DeltaTime);
        //            _angleVelHorz *= curDamp;
        //            _angleVelVert *= curDamp;
        //        }
        //    }

        //    _angleHorz += _angleVelHorz;
        //    _angleVert += _angleVelVert;

        //    // Create the camera matrix and set it as the current View transformation
        //    var mtxRot = float4x4.CreateRotationX(_angleVert) * float4x4.CreateRotationY(_angleHorz);
        //    var mtxCam = float4x4.LookAt(0, 3, -8, 0, 2, 0, 0, 1, 0);
        //    RC.View = mtxCam * mtxRot;

        //    // Tick any animations and Render the scene loaded in Init()
        //    _sceneRenderer.Render(RC);

        //    // Swap buffers: Show the contents of the backbuffer (containing the currently rendered frame) on the front buffer.
        //    Present();
        //}
    }
}