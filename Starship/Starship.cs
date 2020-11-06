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
using System.Security.Cryptography;
using OpenTK.Graphics.OpenGL;
using System.Threading;

namespace FuseeApp
{
    [FuseeApplication(Name = "Starship", Description = "Yet another FUSEE App.")]
    public class Starship : RenderCanvas
    {

        private SceneContainer _starshipScene;
        private SceneRendererForward _sceneRenderer;


        private Transform _starshipTrans;

        private Mesh _starShipMesh;

        private SceneNode TrenchParent;


   
        private float appStartTime;             //Die Zeit seit Start der Applikation
        private float playTime;                 //Die Zeit seit drücken des Startknopfs (Leertaste)             wird später benutzt für das Leaderboard/aktive Anzeige ingame(?)
        private bool start;


        bool left;
        bool mid = true;
        bool right;

        bool up;
        bool normal = true;
        bool down;


        float counter;//1sec

        float3 oldPos;
        float3 newPos;

        //Kollisionsvariablen 
        private AABBf _shipBox; 

        private Transform _cubeObstTrans;
      
        private Mesh _cubeObstMesh;



        List<SceneNode> TrenchesList;


        public override void Init()
        {
            RC.ClearColor = new float4(0, 0, 0, 0);

            _starshipScene = AssetStorage.Get<SceneContainer>("StarshipProto.fus");


            _starshipTrans = _starshipScene.Children.FindNodes(node => node.Name == "Ship")?.FirstOrDefault()?.GetTransform();

            _starShipMesh = _starshipScene.Children.FindNodes(node => node.Name == "Ship")?.FirstOrDefault()?.GetMesh();

            oldPos = _starshipTrans.Translation;
            newPos = _starshipTrans.Translation;


            TrenchesList = new List<SceneNode>
            {
                AssetStorage.Get<SceneContainer>("Trench 1.1.fus").ToSceneNode(),
                AssetStorage.Get<SceneContainer>("Trench 2.fus").ToSceneNode(),
                AssetStorage.Get<SceneContainer>("Trench 3.fus").ToSceneNode()
            };


            int n = TrenchesList.Count();
            Random random = new Random();



            var currentTrench = TrenchesList[0];
            var newTrench = TrenchesList[random.Next(0, n - 1)];

            TrenchParent = new SceneNode()
            {
                Name = "TrenchParent"
            };

            TrenchParent.Children.Add(currentTrench);


            _starshipScene.Children.Add(TrenchParent);


            _sceneRenderer = new SceneRendererForward(_starshipScene);

        }

       

        public override void RenderAFrame()
        {



            // Clear the backbuffer
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);

            RC.Viewport(0, 0, Width, Height);



            double speed = (double)DeltaTime * 20;


 

            //Steuerung links/rechts
            if (Keyboard.IsKeyDown(KeyCodes.Left))
            {
                if (left)
                {
                    mid = false;
                }
                else if (mid)
                {
                    oldPos = newPos;
                    newPos.x = -3;
                    counter = 0.3f;

                    mid = false;
                    left = true;
                    
                }
                else if (right)
                {
                    oldPos = newPos;
                    newPos.x = 0;
                    counter = 0.3f;

                    right = false;
                    mid = true;
                }

            }
            if (Keyboard.IsKeyDown(KeyCodes.Right))
            {
                if (left)
                {
                    oldPos = newPos;
                    newPos.x = 0;
                    counter = 0.3f;

                    left = false;
                    mid = true;
                }
                else if (mid)
                {
                    oldPos = newPos;
                    newPos.x = 3;
                    counter = 0.3f;

                    mid = false;
                    right = true;
                }
                else if (right)
                {
                    mid = false;
                }
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
            //        _starshipTrans.Translation.x += 10 * DeltaTime * Keyboard.LeftRightAxis;

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

            ////Steuerung oben/ unten

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
            //            _starshipTrans.Translation.y -= 7 * DeltaTime * Keyboard.UpDownAxis;
            //        }
            //    }
            //}




            //Start
            //Hier Startbutton / Menü einfügen




            if (Keyboard.IsKeyDown(KeyCodes.Space))  //wenn die Leertaste gedrückt wird, wird die Zeit seit dem Drücken in playTime gespeichert
            {
                start = true;
                appStartTime = RealTimeSinceStart;

                //var irgendwas = AssetStorage.Get<SceneContainer>("Trench 1.fus").ToSceneNode();
                   // _starshipScene.Children.Add(irgendwas);
            }


            //Bounding Boxes 

            //Boundind Box des Schiffs
            _shipBox = _starShipMesh.BoundingBox;




            if (start)
            { 
                //speed *= playTime;            unnötig, aber in ähnlicher Form später für die Geschwindigkeitserhöhung nutzbar
               
                playTime = StartTimer(appStartTime);

                //Hier werden für Trenches sowie ihre jeweiligen obstacles Listen erstellt, die einzeln abgegangen werden, um nach einer Kollision zu prüfen
                for (int j = 0; j < TrenchParent.Children.Count; j++)
                {
                    var _trenchTrans = TrenchParent.Children.ElementAt(j).GetTransform();
                    _trenchTrans.Translation.z -= (float)speed;         //Die Bewegung der Szene wird aktiviert
                    //System.Console.WriteLine(_trenchTrans.Translation);

                    //System.Console.WriteLine(_trenchTrans.Translation);

                    if (_trenchTrans != null)
                    {
                        List<SceneNode> ObstaclesList = TrenchParent.Children.ElementAt(j).Children.FindNodes(node => node.Name.Contains("ObstacleDummy")).ToList();

                        for (int i = 0; i < ObstaclesList.Count(); i++)
                        {
                            _cubeObstTrans = TrenchParent.Children.ElementAt(j).Children.FindNodes(node => node.Name == "ObstacleDummy" + i)?.FirstOrDefault()?.GetTransform();

                            _cubeObstMesh = TrenchParent.Children.ElementAt(j).Children.FindNodes(node => node.Name == "ObstacleDummy" + i)?.FirstOrDefault()?.GetMesh();

                            AABBf cubeHitbox = _trenchTrans.Matrix() * _cubeObstTrans.Matrix() * _cubeObstMesh.BoundingBox;
                            

                            Trench(_shipBox, cubeHitbox);
                        }
                    }
                }
            }
            else
            {
                playTime = 0;  //???? idk ob das so was bringt, aber ist erstmal egal
                speed = 0;
            }


            if (counter > 0)
            {
                _starshipTrans.Translation = float3.Lerp(newPos, oldPos, M.SmootherStep((counter)/0.3f));
               if (newPos.x < oldPos.x)
                {
                    _starshipTrans.Rotation.x = M.SmootherStep(counter / 0.3f) * -0.167f;
                }
               else if (newPos.x > oldPos.x)
                {
                    _starshipTrans.Rotation.x = M.SmootherStep(counter/0.3f) * 0.167f;
                }
                
                counter -= DeltaTime;
            }
            else if (counter < 0)
            {
                counter = 0;
                _starshipTrans.Rotation.x = 0;
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
    
        
        
        private void Trench(AABBf shipHitbox, AABBf cubeHitbox)
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

















    }
}