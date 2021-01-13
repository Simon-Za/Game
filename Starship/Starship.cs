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
using System.IO;

using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using Fusee.Xirkit;
using System.Text;

using System.Security.Cryptography;
using OpenTK.Graphics.OpenGL;
using System.Threading;
using OpenTK.Graphics.ES20;
using Fusee.Engine.Core.Effects;
using Starship.Data;
using System.Xml.Serialization;



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

        private SceneNode _laserbeam;

        private Transform _laserTrans;

        private Mesh _laserMesh;

        private AABBf _laserHitbox;



        private float appStartTime;             //Die Zeit seit Start der Applikation
        private float playTime;                 //Die Zeit seit drücken des Startknopfs (Leertaste)             wird später benutzt für das Leaderboard/aktive Anzeige ingame(?)
        private bool start;
        private double speed;
        private double _fasterSpeedIncr;
        private bool d;
        private bool laser;


        bool left;
        bool mid = true;
        bool right;


        int trenchCount;

        Random random;

        SceneNode currentTrench;
        SceneNode newTrench;

        SceneNode currentItemTrench;
        SceneNode newItemTrench;


        float currentTrenchTrans;


        float counterLR;//1sec
        float counterUD;

        float3 oldPos;
        float3 newPos;

        float oldPosY;
        float newPosY;

        //Kollisionsvariablen 
        private AABBf _shipBox;

        private Transform _cubeObstTrans;

        private Mesh _cubeObstMesh;

        private Transform _itemOrbTrans;

        private Mesh _itemOrbMesh;

        private float _speedIncrItem;

        private SceneNode _currentItem;


        List<SceneNode> TrenchesList;

        List<SceneNode> ItemList;


        private readonly CanvasRenderMode _canvasRenderMode = CanvasRenderMode.Screen;
        private SceneContainer _uiStart;
        private SceneInteractionHandler _sihS;
        private SceneRendererForward _uiStartRenderer;

        private SceneContainer _uiGame;
        private SceneRendererForward _uiGameRenderer;
        private SceneInteractionHandler _sihG;

        private GUIText _timerText;
        private GUIText _ldrbrdText;

        private SceneContainer _uiDeath;
        private SceneRendererForward _uiDeathRenderer;
        private SceneInteractionHandler _sihD;

        private const float ZNear = 1f;
        private const float ZFar = 1000;


        //private enum Status {Start, Game, Death};
        private int status = 0;

        List<Score> ScoresList;

        private double currentScore;

        //private string path = @"c:\temp\Leaderboard.txt";
        private string path = @"C:\Users\symz1\Documents\FUSEE\Game\Starship\bin\Debug\Leaderboard.txt";

        private int _itemStatus; //0 = nichts, 1 = invincibility, 2 = ??

        private float _itemTimer;

        private float4 _color;
       



        public override void Init()
        {
            RC.ClearColor = new float4(0.3f, 0, 0.9f, 0);


            _uiStart = CreateUIStart();
            _uiGame = CreateUIGame();
            _uiDeath = CreateUIDeath();

            //Create the interaction handler
            _sihS = new SceneInteractionHandler(_uiStart);
            _sihG = new SceneInteractionHandler(_uiGame);
            _sihD = new SceneInteractionHandler(_uiDeath);


            _starshipScene = AssetStorage.Get<SceneContainer>("StarshipProto.fus");


            _starshipTrans = _starshipScene.Children.FindNodes(node => node.Name == "Ship")?.FirstOrDefault()?.GetTransform();

            _starShipMesh = _starshipScene.Children.FindNodes(node => node.Name == "Ship")?.FirstOrDefault()?.GetMesh();

            _laserbeam = AssetStorage.Get<SceneContainer>("Laserbeam.fus").ToSceneNode();

            _laserTrans = _laserbeam.GetTransform();

            _laserMesh = _laserbeam.Children[0].GetMesh();



            oldPos = _starshipTrans.Translation;
            newPos = _starshipTrans.Translation;

            oldPosY = _starshipTrans.Translation.y;
            newPosY = _starshipTrans.Translation.y;


            TrenchesList = new List<SceneNode>
            {
               AssetStorage.Get<SceneContainer>("CubesOnly1.fus").ToSceneNode(),
               AssetStorage.Get<SceneContainer>("CubesOnly2.fus").ToSceneNode(),
               AssetStorage.Get<SceneContainer>("CubesOnly3.fus").ToSceneNode()
            };

            ItemList = new List<SceneNode>
            {
                AssetStorage.Get<SceneContainer>("ItemTrench1.fus").ToSceneNode(),
                AssetStorage.Get<SceneContainer>("ItemTrench2.fus").ToSceneNode(),
            };

            random = new Random();
            trenchCount = TrenchesList.Count();

            currentTrench = CopyNode(TrenchesList[0]);
            newTrench = CopyNode(TrenchesList[random.Next(0, trenchCount)]);

            currentItemTrench = CopyNode(ItemList[0]);
            newItemTrench = CopyNode(ItemList[random.Next(0, ItemList.Count())]);

            newTrench.GetTransform().Translation.z += newTrench.GetTransform().Scale.z;

            _speedIncrItem = 1;
            _fasterSpeedIncr = 1;

            laser = false;

            TrenchParent = new SceneNode()
            {
                Name = "TrenchParent"
            };

            _starshipScene.Children.Add(TrenchParent);

            TrenchParent.Children.Add(currentTrench);
            TrenchParent.Children.Add(newTrench);

            TrenchParent.Children.Add(currentItemTrench);
            TrenchParent.Children.Add(newItemTrench);


            currentTrenchTrans = currentTrench.GetTransform().Translation.z;


            _sceneRenderer = new SceneRendererForward(_starshipScene);
            _uiStartRenderer = new SceneRendererForward(_uiStart);
            _uiGameRenderer = new SceneRendererForward(_uiGame);
            _uiDeathRenderer = new SceneRendererForward(_uiDeath);


            _color = (float4)_starshipScene.Children.FindNodes(node => node.Name == "Ship")?.FirstOrDefault()?.GetComponent<DefaultSurfaceEffect>().GetFxParam<float4>("SurfaceInput.Albedo");

        }


        public override void RenderAFrame()
        {

            // Clear the backbuffer
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);

            RC.Viewport(0, 0, Width, Height);


            //Item Shenanigans
            if (_itemTimer <= playTime && _itemOrbMesh != null)
            {
                _itemOrbMesh.Active = true;
            }
            if (_itemTimer <= playTime)
            {
                _itemStatus = 0;
            }

            if(_itemStatus == 3)
            {
                appStartTime -= 1;
            }
            else if (_itemStatus == 4)
            {
                _speedIncrItem = 0.95f;
            }
            else
            {
                _speedIncrItem = 1.0526f;
                if(_itemOrbMesh!= null)
                _itemOrbMesh.Active = true;
            }


            if(_itemStatus == 5)
            {
                if(laser == false)
                {
                    _starshipScene.Children.Add(_laserbeam);
                    laser = true;
                }
                _laserTrans.Translation.y = _starshipTrans.Translation.y - 2;
                _laserTrans.Translation.x = _starshipTrans.Translation.x;




                if(_itemTimer <= playTime + 2 /*&& _starshipScene.Children.FindNodes(node => node.Name == "Laserbeam") != null*/)
                {
                    _starshipScene.Children.Remove(_laserbeam);
                }
            }
            else
            {
                laser = false;
                _starshipScene.Children.Remove(_laserbeam);
            }


            if (_itemStatus == 1)
            {
                _starshipScene.Children.FindNodes(node => node.Name == "Ship")?.FirstOrDefault()?.GetComponent<DefaultSurfaceEffect>().SetFxParam("SurfaceInput.Albedo", new float4(3, 3, 0, 1));
            }
            else if (_itemStatus == 2)
            {
                _starshipScene.Children.FindNodes(node => node.Name == "Ship")?.FirstOrDefault()?.GetComponent<DefaultSurfaceEffect>().SetFxParam("SurfaceInput.Albedo", new float4(1, 2, 3, 1));
            }
            else if (_itemStatus == 3)
            {
                _starshipScene.Children.FindNodes(node => node.Name == "Ship")?.FirstOrDefault()?.GetComponent<DefaultSurfaceEffect>().SetFxParam("SurfaceInput.Albedo", new float4(3, 3, 3, 1));
            }
            else if (_itemStatus == 4)
            {
                _starshipScene.Children.FindNodes(node => node.Name == "Ship")?.FirstOrDefault()?.GetComponent<DefaultSurfaceEffect>().SetFxParam("SurfaceInput.Albedo", new float4(3, 1, 3, 1));
            }
            else
            {
                _starshipScene.Children.FindNodes(node => node.Name == "Ship")?.FirstOrDefault()?.GetComponent<DefaultSurfaceEffect>().SetFxParam("SurfaceInput.Albedo", _color);
            };



            if (playTime != 0)
            {
                if ((int)playTime % 20 == 0 && (int)playTime != 0 && d == true)
                {
                    Faster();
                    d = false;
                }
                else if ((int)playTime % 20 != 0 && d == false)
                {
                    d = true;
                }
            }


            //Trench Switches
            if (newTrench.GetTransform().Translation.z <= currentTrenchTrans)
            {
                TrenchParent.Children.Remove(currentTrench);
                TrenchParent.Children.Remove(newTrench);

                TrenchParent.Children.Remove(currentItemTrench);
                TrenchParent.Children.Remove(newItemTrench);


                currentTrench = newTrench;
                newTrench = CopyNode(TrenchesList[random.Next(0, trenchCount)]);
                newTrench.GetTransform().Translation.z = 99;
                TrenchParent.Children.Add(currentTrench);
                TrenchParent.Children.Add(newTrench);

                currentItemTrench = newItemTrench;
                newItemTrench = CopyNode(ItemList[random.Next(0, ItemList.Count())]);
                newItemTrench.GetTransform().Translation.z = 99;
                TrenchParent.Children.Add(currentItemTrench);
                TrenchParent.Children.Add(newItemTrench);
            }



            //Steuerung links/rechts
            if (Keyboard.IsKeyDown(KeyCodes.Left) || Keyboard.IsKeyDown(KeyCodes.A))
            {
                if (left)
                {
                    mid = false;
                }
                else if (mid)
                {
                    oldPos = newPos;
                    newPos.x = -3;
                    counterLR = 0.3f;

                    mid = false;
                    left = true;

                }
                else if (right)
                {
                    oldPos = newPos;
                    newPos.x = 0;
                    counterLR = 0.3f;

                    right = false;
                    mid = true;
                }

            }
            if (Keyboard.IsKeyDown(KeyCodes.Right) || Keyboard.IsKeyDown(KeyCodes.D))
            {
                if (left)
                {
                    oldPos = newPos;
                    newPos.x = 0;
                    counterLR = 0.3f;

                    left = false;
                    mid = true;
                }
                else if (mid)
                {
                    oldPos = newPos;
                    newPos.x = 3;
                    counterLR = 0.3f;

                    mid = false;
                    right = true;
                }
                else if (right)
                {
                    mid = false;
                }
            }


            //oben / unten
            if (Keyboard.IsKeyDown(KeyCodes.Up) && counterUD == 0 || Keyboard.IsKeyDown(KeyCodes.W) && counterUD == 0)
            {               
                oldPosY = newPosY;
                newPosY = 4.2039146f;
                counterUD = 0.3f;

            }

            if (Keyboard.IsKeyDown(KeyCodes.Down) && counterUD == 0 || Keyboard.IsKeyDown(KeyCodes.S) && counterUD == 0)
            {  
                oldPosY = newPosY;
                newPosY = -0.7960852f;
                counterUD = 0.3f;
            }



            //Start und Restart Button

            if (status == 0)
            {
                if (Keyboard.IsKeyDown(KeyCodes.Enter))
                {
                    StartGame();

                }
            }
            else if ( status == 2)
            {
                if (Keyboard.IsKeyDown(KeyCodes.Enter))
                {
                    TryAgain();
                }
            }
            if(_itemStatus == 4 && _itemTimer == playTime + 3)
            {
                speed = ((double)DeltaTime * 20) *_speedIncrItem * _fasterSpeedIncr;
            }
            else if(_itemStatus == 0)
            {
                speed = ((double)DeltaTime * 20) * _speedIncrItem * _fasterSpeedIncr;
            }

            //Bounding Boxes and collision detection

            //Boundind Box des Schiffs  
            _shipBox = _starshipTrans.Matrix() * _starShipMesh.BoundingBox;

            if (start)
            {
                playTime = StartTimer(appStartTime);

                //Hier werden für Trenches sowie ihre jeweiligen obstacles Listen erstellt, die einzeln abgegangen werden, um nach einer Kollision zu prüfen
                for (int j = 0; j < TrenchParent.Children.Count; j++)
                {
                    var _trenchTrans = TrenchParent.Children.ElementAt(j).GetTransform();
                    _trenchTrans.Translation.z -= (float)speed;         //Die Bewegung der Szene wird aktiviert


                    if (_trenchTrans != null)
                    {
                        List<SceneNode> ObstaclesList = TrenchParent.Children.ElementAt(j).Children.FindNodes(node => node.Name.Contains("CubeObstacle")).ToList();
                        
                        List<SceneNode> ItemList = TrenchParent.Children.ElementAt(j).Children.FindNodes(node => node.Name.Contains("ItemOrb")).ToList();


                        if (_itemStatus != 1) //Wenn Invinvibility nicht aktiv ist
                        {

                            for (int i = 0; i < ObstaclesList.Count(); i++)
                            {
                                _cubeObstTrans = TrenchParent.Children.ElementAt(j).Children.FindNodes(node => node.Name == "CubeObstacle" + i)?.FirstOrDefault()?.GetTransform();

                                _cubeObstMesh = TrenchParent.Children.ElementAt(j).Children.FindNodes(node => node.Name == "CubeObstacle" + i)?.FirstOrDefault()?.GetMesh();

                                AABBf cubeHitbox = _trenchTrans.Matrix() * _cubeObstTrans.Matrix() * _cubeObstMesh.BoundingBox;

                                if (_itemStatus == 5)
                                {
                                    _laserHitbox = _laserTrans.Matrix() * _laserMesh.BoundingBox;
                                    LaserCollision(_laserHitbox, cubeHitbox);
                                }

                                if (_itemStatus == 2) //Wenn Shield aktiv ist
                                {
                                    ShieldCollision(_shipBox, cubeHitbox);
                                }
                                else
                                {
                                    _cubeObstMesh.Active = true;
                                    Collision(_shipBox, cubeHitbox);
                                }
                            }
                        }

                        for (int i = 0; i < ItemList.Count(); i++)
                        {

                            _currentItem = TrenchParent.Children.ElementAt(j).Children.FindNodes(node => node.Name == "ItemOrb" + i)?.FirstOrDefault();
                            _itemOrbTrans = TrenchParent.Children.ElementAt(j).Children.FindNodes(node => node.Name == "ItemOrb" + i)?.FirstOrDefault()?.GetTransform();
                            _itemOrbMesh = TrenchParent.Children.ElementAt(j).Children.FindNodes(node => node.Name == "ItemOrb" + i)?.FirstOrDefault()?.GetMesh();

                            AABBf itemHitbox = _trenchTrans.Matrix() * _itemOrbTrans.Matrix() * _itemOrbMesh.BoundingBox;

                            if (_itemOrbMesh.Active == true)
                            {
                                ObtainItem(_shipBox, itemHitbox);
                            }
                        }
                    }
                }
            }
            else
            {
                speed = 0;
            }

            if (counterLR > 0)        //Bewegung und Tilt
            {
                _starshipTrans.Translation = float3.Lerp(newPos, oldPos, M.SmootherStep((counterLR) / 0.3f));
                if (newPos.x < oldPos.x)
                {
                    _starshipTrans.Rotation.x = M.SmootherStep(counterLR / 0.3f) * -0.167f;
                }
                else if (newPos.x > oldPos.x)
                {
                    _starshipTrans.Rotation.x = M.SmootherStep(counterLR / 0.3f) * 0.167f;
                }

                counterLR -= DeltaTime;
            }
            else if (counterLR < 0)
            {
                counterLR = 0;
                _starshipTrans.Rotation.x = 0;
            }



            float3 newPosXY = new float3(_starshipTrans.Translation.x, newPosY, _starshipTrans.Translation.z);
            float3 oldPosXY = new float3(_starshipTrans.Translation.x, oldPosY, _starshipTrans.Translation.z);

            if (counterUD > 0)        //Bewegung und Tilt Oben/Unten
            {
                _starshipTrans.Translation = float3.Lerp(newPosXY, oldPosXY, M.SmootherStep((counterUD) / 0.3f));
                newPos.y = newPosY;
                oldPos.y = newPosY;
                if (newPosY < oldPosY)
                {
                    _starshipTrans.Rotation.z = M.SmootherStep(counterUD / 0.3f) * 1;//0.167f;
                }
                else if (newPosY > oldPosY)
                {
                    _starshipTrans.Rotation.z = M.SmootherStep(counterUD / 0.3f) * -1; // 0.167f;
                }

                counterUD -= DeltaTime;

            }
            else if (counterUD < 0 && counterUD > -0.2f)
            {
                counterUD -= DeltaTime;
            }
            else if (counterUD < -0.2f && counterUD >= -0.5f)
            {
                _starshipTrans.Translation = float3.Lerp(newPosXY, oldPosXY, M.SmootherStep((counterUD + 0.2f) / -0.3f));

                if (oldPosY < newPosY)
                {
                    _starshipTrans.Rotation.z = M.SmoothStep(-(counterUD + 0.2f) / 0.3f) * 0.5f;// 1.5f; //-0.167f;
                }
                else if (oldPosY > newPosY)
                {
                    _starshipTrans.Rotation.z = M.SmoothStep(-(counterUD + 0.2f) / 0.3f) * -0.7f; // 1.5f;//0.167f;
                }
                newPos.y = oldPosY;
                oldPos.y = newPosY;

                counterUD -= DeltaTime;
            }
            else if (counterUD < -0.5f)
            {
                newPosY = oldPosY;
                counterUD = 0;
                _starshipTrans.Rotation.z = 0;
            }


            RC.View = float4x4.LookAt(0, 8, -8, 0, 0, 7, 0, 1, 0);



            //Tick any animations and Render the scene loaded in Init()

            var orthographic = float4x4.CreateOrthographic(Width, Height, ZNear, ZFar);

            _sceneRenderer.Render(RC);

            RC.Projection = orthographic;

            //Console.WriteLine(playTime.ToString());


            _timerText.Text = playTime.ToString();
            //Console.WriteLine(speed);
            Console.WriteLine(_itemStatus);

         
            //verschiedene UIs werden gerendert
            if (status == 0)
            {
                _uiStartRenderer.Render(RC);
                _sihS.CheckForInteractiveObjects(RC, Mouse.Position, Width, Height);
            }
            else if(status == 1)
            {
                _uiGameRenderer.Render(RC);
            }
            else if(status == 2)
            {
                _uiGameRenderer.Render(RC);
                _uiDeathRenderer.Render(RC);
                _sihD.CheckForInteractiveObjects(RC, Mouse.Position, Width, Height);
            }

            // Swap buffers: Show the contents of the backbuffer (containing the currently rendered frame) on the front buffer.
            Present();

        }


        private SceneContainer CreateUIStart()      //UI für Start mit Enter
        {
            var vsTex = AssetStorage.Get<string>("texture.vert");
            var psTex = AssetStorage.Get<string>("texture.frag");
            var vsNineSlice = AssetStorage.Get<string>("nineSlice.vert");
            var psNineSlice = AssetStorage.Get<string>("nineSliceTile.frag");
            var psText = AssetStorage.Get<string>("text.frag");

            var canvasWidth = Width / 100f;
            var canvasHeight = Height / 100f;

            var fontLato = AssetStorage.Get<Font>("Lato-Black.ttf");
            var guiLatoBlack = new FontMap(fontLato, 36);


            var startText = new TextNode(
                "Press Enter to start",
                "StartText",
                vsTex,
                psText,
                UIElementPosition.GetAnchors(AnchorPos.StretchHorizontal),
                UIElementPosition.CalcOffsets(AnchorPos.StretchHorizontal, new float2(canvasWidth / 2 - 4.3f, 0), canvasHeight, canvasWidth, new float2(8.5f, 7.5f)),
                guiLatoBlack,
                ColorUint.Tofloat4(ColorUint.White),
                HorizontalTextAlignment.Center,
                VerticalTextAlignment.Center);


            var canvas = new CanvasNode(
                "Canvas",
                _canvasRenderMode,
                new MinMaxRect
                {

                    Min = new float2(-canvasWidth / 2, -canvasHeight / 2f),
                    Max = new float2(canvasWidth / 2, canvasHeight / 2f)
                })
            {
                Children = new ChildList()
                {
                    startText
                }
            };

            return new SceneContainer
            {
                Children = new List<SceneNode>
                {
                    canvas
                }
            };
        }


        private SceneContainer CreateUIGame() //UI für ingame Zeit
        {
            var vsTex = AssetStorage.Get<string>("texture.vert");
            var psTex = AssetStorage.Get<string>("texture.frag");
            var vsNineSlice = AssetStorage.Get<string>("nineSlice.vert");
            var psNineSlice = AssetStorage.Get<string>("nineSliceTile.frag");
            var psText = AssetStorage.Get<string>("text.frag");

            var canvasWidth = Width / 100f;
            var canvasHeight = Height / 100f;

            var fontLato = AssetStorage.Get<Font>("Lato-Black.ttf");
            var guiLatoBlack = new FontMap(fontLato, 24);


            var displayTime = new TextNode(
                "0.00",
                "TimerText",
                vsTex,
                psText,
                UIElementPosition.GetAnchors(AnchorPos.StretchHorizontal),
                UIElementPosition.CalcOffsets(AnchorPos.StretchHorizontal, new float2(canvasWidth / 2 - 4.3f, 0), canvasHeight, canvasWidth, new float2(8, 1)),
                guiLatoBlack,
                ColorUint.Tofloat4(ColorUint.Green),
                HorizontalTextAlignment.Center,
                VerticalTextAlignment.Center);

            _timerText = displayTime.GetComponentsInChildren<GUIText>().FirstOrDefault();


            var canvas = new CanvasNode(
                "Canvas",
                _canvasRenderMode,
                new MinMaxRect
                {
                    Min = new float2(-canvasWidth / 2, -canvasHeight / 2f),
                    Max = new float2(canvasWidth / 2, canvasHeight / 2f)
                })
            {
                Children = new ChildList()
                {
                    displayTime
                }
            };

            return new SceneContainer
            {
                Children = new List<SceneNode>
                {
                    canvas
                }
            };
        }

        private SceneContainer CreateUIDeath() //UI für Todesscreen
        {

            var vsTex = AssetStorage.Get<string>("texture.vert");
            var psTex = AssetStorage.Get<string>("texture.frag");
            var vsNineSlice = AssetStorage.Get<string>("nineSlice.vert");
            var psNineSlice = AssetStorage.Get<string>("nineSliceTile.frag");
            var psText = AssetStorage.Get<string>("text.frag");

            var canvasWidth = Width / 100f;
            var canvasHeight = Height / 100f;

            var fontLato = AssetStorage.Get<Font>("Lato-Black.ttf");
            var guiLatoBlack = new FontMap(fontLato, 36);


            var leaderboardText = new TextNode(
                "Leaderboard",
                "LeaderboardText",
                vsTex,
                psText,
                UIElementPosition.GetAnchors(AnchorPos.StretchHorizontal),
                UIElementPosition.CalcOffsets(AnchorPos.StretchHorizontal, new float2(canvasWidth / 2 - 4.3f, canvasHeight - 4.3f), canvasHeight, canvasWidth, new float2(8.5f, 7.5f)),
                guiLatoBlack,
                ColorUint.Tofloat4(ColorUint.White),
                HorizontalTextAlignment.Center,
                VerticalTextAlignment.Center);

            _ldrbrdText = leaderboardText.GetComponentsInChildren<GUIText>().FirstOrDefault();


            var restartText = new TextNode(
                "Press Enter to retry",
                "RestartText",
                vsTex,
                psText,
                UIElementPosition.GetAnchors(AnchorPos.StretchHorizontal),
                UIElementPosition.CalcOffsets(AnchorPos.StretchHorizontal, new float2(canvasWidth / 2 - 4.3f, canvasHeight / -2.4f), canvasHeight, canvasWidth, new float2(8.5f, 7.7f)),
                guiLatoBlack,
                ColorUint.Tofloat4(ColorUint.Red),
                HorizontalTextAlignment.Center,
                VerticalTextAlignment.Center);



            var canvas = new CanvasNode(
                "Canvas",
                _canvasRenderMode,
                new MinMaxRect
                {
                    Min = new float2(-canvasWidth / 2, -canvasHeight / 2f),
                    Max = new float2(canvasWidth / 2, canvasHeight / 2f)
                })
            {
                Children = new ChildList()
                {
                    leaderboardText,
                    restartText,
                }
            };

            return new SceneContainer
            {
                Children = new List<SceneNode>
                {
                    canvas
                }
            };
        }





        //Start des Spiels, also Beginn der Bewegung der Szene und des Zählers
        private void StartGame()
        {
            start = true;
            appStartTime = RealTimeSinceStart;
            speed = (double)DeltaTime * 20;
            _speedIncrItem = 1;
            _fasterSpeedIncr = 1;
            status = 1;
        }

        private void TryAgain()
        {
            ReloadScene();
            StartGame();
        }

        private void ReloadScene()
        {
            TrenchParent.Children.Remove(currentTrench);
            TrenchParent.Children.Remove(newTrench);

            currentTrench = CopyNode(TrenchesList[0]);
            newTrench = CopyNode(TrenchesList[random.Next(0, trenchCount)]);

            newTrench.GetTransform().Translation.z += newTrench.GetTransform().Scale.z;


            TrenchParent.Children.Add(currentTrench);
            TrenchParent.Children.Add(newTrench);


            currentTrenchTrans = currentTrench.GetTransform().Translation.z;
            _itemStatus = 0;
        }

        //Timer wird gestartet
        private float StartTimer(float appStartTime)
        {
            return RealTimeSinceStart - appStartTime;        
        }
    
       

        private void Collision(AABBf _shipBox, AABBf cubeHitbox)
        {
            if (_shipBox.Intersects(cubeHitbox))
            {

                Console.WriteLine("Boom!");
                Death();
            }  
        }

        private void ShieldCollision(AABBf _shipBox, AABBf cubeHitbox)
        {
            if (_shipBox.Intersects(cubeHitbox))
            {
                _cubeObstMesh.Active = false;
                _itemTimer = playTime + 0.2f;
            }
        }

        private void LaserCollision(AABBf _laserHitbox, AABBf cubeHitbox)
        {
            if (_laserHitbox.Intersects(cubeHitbox))
            {
                _cubeObstMesh.Active = false;
            }
        }

        private void ObtainItem(AABBf _shipBox, AABBf itemHitbox)
        {
            if (itemHitbox.Intersects(_shipBox.Center))
            {               
                _itemOrbMesh.Active = false;
                random = new Random();
                _itemStatus = random.Next(1, 6);    //hier random item 1-x
                if(_itemStatus != 2 && _itemStatus != 3)
                {
                    _itemTimer = playTime + 3;//(float)speed * 60 / playTime;
                }
                else if (_itemStatus == 2)
                {
                    _itemTimer = playTime + 20;    //lange Zeit für Schild, Timer wird runtergesetzt bei Kollision
                }
                else if (_itemStatus == 3)
                {
                    _itemTimer = playTime + 1;
                }
                
                Console.WriteLine(_itemTimer);
            }
        }


        private void Death()
        {
            currentScore = playTime;
            Leaderboard();
            start = false;
            status = 2;
            _ldrbrdText.Text = "Leaderboard";
            for (int m = 0; m < 10; m++)
            {
                _ldrbrdText.Text += Environment.NewLine +  Math.Round((ScoresList[m].topTime), 3).ToString();
            }
        }



        private SceneNode CopyNode(SceneNode insn)
        {
            SceneNode outsn = new SceneNode();

            outsn.Name = insn.Name;
            outsn.Components.Add(new Transform()); 
            outsn.Children = insn.Children;

            //var c = MakeEffect.Default;       Standard Shader

            return outsn;
        }

        private void Faster()
        {
            _fasterSpeedIncr *= 1.2f; 
        }

        private void Leaderboard()
        {
            var blub = new Leaderboard();
            var ser = new XmlSerializer(typeof(Leaderboard));
            

            if(!File.Exists("Leaderboard.xml"))
            {
                blub.Scores = new List<Score>
                {
                    new Score(0.000)
                };

                using StringWriter TextWriter = new StringWriter();
                ser.Serialize(TextWriter, blub);
                File.WriteAllText("Leaderboard.xml", TextWriter.ToString());
                TextWriter.Dispose();
            }
           
            TextReader reader = new StreamReader("Leaderboard.xml");
            object obj = ser.Deserialize(reader);
            blub = (Leaderboard)obj;


            for (int k = 0; k < blub.Scores.Count(); k++)
            {
                
                if (currentScore >= blub.Scores.ElementAt(k).topTime)
                {
                    blub.Scores.Insert(k, new Score(currentScore));
                    break;
                }
            }
            ScoresList = blub.Scores;
            reader.Dispose();

            for (int l = 0; l < blub.Scores.Count() && l < 10 ; l++)
            {
                Console.WriteLine(blub.Scores[l].topTime);
            }


            using StringWriter TextWriter2 = new StringWriter();
            ser.Serialize(TextWriter2, blub);
            File.WriteAllText("Leaderboard.xml", TextWriter2.ToString());
            TextWriter2.Dispose();

        }
    }
}