﻿using Fusee.Base.Common;
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

        private AABBf _laserBB;

        private AABBf _laserHitbox;

        private float _laserBBYMin;
        private float _laserBBYMax;



        private float _appStartTime;             //Die Zeit seit Start der Applikation
        private float _playTime;                 //Die Zeit seit drücken des Startknopfs (Leertaste)             wird später benutzt für das Leaderboard/aktive Anzeige ingame(?)
        private bool _start;
        private double _speed;
        private double _fasterSpeedIncr;
        private bool _d;
        private bool _laser;


        bool _left;
        bool _mid = true;
        bool _right;


        int _trenchCount;

        Random _random;

        SceneNode _currentTrench;
        SceneNode _newTrench;

        SceneNode _currentItemTrench;
        SceneNode _newItemTrench;


        float _currentTrenchTrans;


        float _counterLR;//1sec
        float _counterUD;

        float3 _oldPos;
        float3 _newPos;

        float _oldPosY;
        float _newPosY;

        //Kollisionsvariablen 
        private AABBf _shipBox;

        private Transform _cubeObstTrans;

        private Mesh _cubeObstMesh;

        private Transform _itemOrbTrans;

        private Mesh _itemOrbMesh;

        private float _speedIncrItem;

        private SceneNode _currentItem;


        List<SceneNode> _trenchesList;

        List<SceneNode> _itemList;


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


            _starshipScene = AssetStorage.Get<SceneContainer>("DeloreanV2smol.fus");


            _starshipTrans = _starshipScene.Children.FindNodes(node => node.Name == "Chassis")?.FirstOrDefault()?.GetTransform();

            _starShipMesh = _starshipScene.Children.FindNodes(node => node.Name == "Chassis")?.FirstOrDefault()?.GetMesh();

            _laserbeam = AssetStorage.Get<SceneContainer>("Laserbeam2.fus").ToSceneNode();

            _laserTrans = _laserbeam.GetTransform();

            _laserMesh = _laserbeam.Children[0].GetMesh();

            _laserBB = _laserbeam.Children[0].GetMesh().BoundingBox;

            _laserBBYMin = _laserBB.min.y;
            _laserBB.min.y = _laserBB.min.z;
            _laserBB.min.z = _laserBBYMin;
            _laserBB.min.z += _laserBB.Size.z / 2;

            _laserBBYMax = _laserBB.max.y;
            _laserBB.max.y = _laserBB.max.z;
            _laserBB.max.z = _laserBBYMax;
            _laserBB.max.z += _laserBB.Size.z / 2;




            _oldPos = _starshipTrans.Translation;
            _newPos = _starshipTrans.Translation;

            _oldPosY = _starshipTrans.Translation.y;
            _newPosY = _starshipTrans.Translation.y;


            _trenchesList = new List<SceneNode>
            {
                AssetStorage.Get<SceneContainer>("TestCarTrench1v2.fus").ToSceneNode(),
                AssetStorage.Get<SceneContainer>("TestCarTrench2v2.fus").ToSceneNode(),
               //AssetStorage.Get<SceneContainer>("CarTrench1v1.fus").ToSceneNode(),
               //AssetStorage.Get<SceneContainer>("CarTrench2v1.fus").ToSceneNode(),
               //AssetStorage.Get<SceneContainer>("CubesOnly3.fus").ToSceneNode()
            };

            _itemList = new List<SceneNode>
            {
                AssetStorage.Get<SceneContainer>("ItemTrench1.fus").ToSceneNode(),
                AssetStorage.Get<SceneContainer>("ItemTrench2.fus").ToSceneNode(),
            };

            _random = new Random();
            _trenchCount = _trenchesList.Count();

            _currentTrench = CopyNode(_trenchesList[0]);
            _newTrench = CopyNode(_trenchesList[_random.Next(0, _trenchCount)]);

            _currentItemTrench = CopyNode(_itemList[0]);
            _newItemTrench = CopyNode(_itemList[_random.Next(0, _itemList.Count())]);

            _newTrench.GetTransform().Translation.z += _newTrench.GetTransform().Scale.z;

            _speedIncrItem = 1;
            _fasterSpeedIncr = 1;

            _laser = false;

            TrenchParent = new SceneNode()
            {
                Name = "TrenchParent"
            };

            _starshipScene.Children.Add(TrenchParent);

            TrenchParent.Children.Add(_currentTrench);
            TrenchParent.Children.Add(_newTrench);

            TrenchParent.Children.Add(_currentItemTrench);
            TrenchParent.Children.Add(_newItemTrench);


            _currentTrenchTrans = _currentTrench.GetTransform().Translation.z;


            _sceneRenderer = new SceneRendererForward(_starshipScene);
            _uiStartRenderer = new SceneRendererForward(_uiStart);
            _uiGameRenderer = new SceneRendererForward(_uiGame);
            _uiDeathRenderer = new SceneRendererForward(_uiDeath);


            _color = (float4)_starshipScene.Children.FindNodes(node => node.Name == "Chassis")?.FirstOrDefault()?.GetComponent<DefaultSurfaceEffect>().GetFxParam<float4>("SurfaceInput.Albedo");

        }


        public override void RenderAFrame()
        {

            // Clear the backbuffer
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);

            RC.Viewport(0, 0, Width, Height);


            //Item Shenanigans
            if (_itemTimer <= _playTime && _itemOrbMesh != null)
            {
                _itemOrbMesh.Active = true;
                _itemOrbMesh.BoundingBox.min.y = -0.6f;
            }
            if (_itemTimer <= _playTime)
            {
                _itemStatus = 0;
            }

            if(_itemStatus == 3)
            {
                _appStartTime -= 1;
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
                if (_laser == false)
                {
                    _starshipScene.Children.Add(_laserbeam);
                    _laser = true;
                }
                _laserTrans.Translation.y = _starshipTrans.Translation.y - 0.5f;
                _laserTrans.Translation.x = _starshipTrans.Translation.x;




                if(_itemTimer <= _playTime + 2 /*&& _starshipScene.Children.FindNodes(node => node.Name == "Laserbeam") != null*/)
                {
                    _starshipScene.Children.Remove(_laserbeam);
                }
            }
            else
            {
                _laser = false;
                _starshipScene.Children.Remove(_laserbeam);
            }


            if (_itemStatus == 1)
            {
                _starshipScene.Children.FindNodes(node => node.Name == "Chassis")?.FirstOrDefault()?.GetComponent<DefaultSurfaceEffect>().SetFxParam("SurfaceInput.Albedo", new float4(3, 3, 0, 1));
            }
            else if (_itemStatus == 2)
            {
                _starshipScene.Children.FindNodes(node => node.Name == "Chassis")?.FirstOrDefault()?.GetComponent<DefaultSurfaceEffect>().SetFxParam("SurfaceInput.Albedo", new float4(1, 2, 3, 1));
            }
            else if (_itemStatus == 3)
            {
                _starshipScene.Children.FindNodes(node => node.Name == "Chassis")?.FirstOrDefault()?.GetComponent<DefaultSurfaceEffect>().SetFxParam("SurfaceInput.Albedo", new float4(3, 3, 3, 1));
            }
            else if (_itemStatus == 4)
            {
                _starshipScene.Children.FindNodes(node => node.Name == "Chassis")?.FirstOrDefault()?.GetComponent<DefaultSurfaceEffect>().SetFxParam("SurfaceInput.Albedo", new float4(3, 1, 3, 1));
            }
            else
            {
                _starshipScene.Children.FindNodes(node => node.Name == "Chassis")?.FirstOrDefault()?.GetComponent<DefaultSurfaceEffect>().SetFxParam("SurfaceInput.Albedo", _color);
            };



            if (_playTime != 0)
            {
                if ((int)_playTime % 20 == 0 && (int)_playTime != 0 && _d == true)
                {
                    Faster();
                    _d = false;
                }
                else if ((int)_playTime % 20 != 0 && _d == false)
                {
                    _d = true;
                }
            }


            //Trench Switches
            if (_newTrench.GetTransform().Translation.z <= _currentTrenchTrans)
            {
                TrenchParent.Children.Remove(_currentTrench);
                TrenchParent.Children.Remove(_newTrench);

                TrenchParent.Children.Remove(_currentItemTrench);
                TrenchParent.Children.Remove(_newItemTrench);


                _currentTrench = _newTrench;
                _newTrench = CopyNode(_trenchesList[_random.Next(0, _trenchCount)]);
                _newTrench.GetTransform().Translation.z = 99;
                TrenchParent.Children.Add(_currentTrench);
                TrenchParent.Children.Add(_newTrench);

                _currentItemTrench = _newItemTrench;
                _newItemTrench = CopyNode(_itemList[_random.Next(0, _itemList.Count())]);
                _newItemTrench.GetTransform().Translation.z = 99;
                TrenchParent.Children.Add(_currentItemTrench);
                TrenchParent.Children.Add(_newItemTrench);
            }       //wenn Trenches länger werden, hier magische Zahlen ändern!!



            //Steuerung links/rechts
            if (Keyboard.IsKeyDown(KeyCodes.Left) || Keyboard.IsKeyDown(KeyCodes.A))
            {
                if (_left)
                {
                    _mid = false;
                }
                else if (_mid)
                {
                    _oldPos = _newPos;
                    _newPos.x = -3;
                    _counterLR = 0.3f;

                    _mid = false;
                    _left = true;

                }
                else if (_right)
                {
                    _oldPos = _newPos;
                    _newPos.x = 0;
                    _counterLR = 0.3f;

                    _right = false;
                    _mid = true;
                }

            }
            if (Keyboard.IsKeyDown(KeyCodes.Right) || Keyboard.IsKeyDown(KeyCodes.D))
            {
                if (_left)
                {
                    _oldPos = _newPos;
                    _newPos.x = 0;
                    _counterLR = 0.3f;

                    _left = false;
                    _mid = true;
                }
                else if (_mid)
                {
                    _oldPos = _newPos;
                    _newPos.x = 3;
                    _counterLR = 0.3f;

                    _mid = false;
                    _right = true;
                }
                else if (_right)
                {
                    _mid = false;
                }
            }


            //oben / unten
            if (Keyboard.IsKeyDown(KeyCodes.Up) && _counterUD == 0 || Keyboard.IsKeyDown(KeyCodes.W) && _counterUD == 0)
            {
                _oldPosY = _newPosY;
                _newPosY = 4.2039146f;
                _counterUD = 0.3f;

            }

            if (Keyboard.IsKeyDown(KeyCodes.Down) && _counterUD == 0 || Keyboard.IsKeyDown(KeyCodes.S) && _counterUD == 0)
            {
                _oldPosY = _newPosY;
                _newPosY = -0.7960852f;
                _counterUD = 0.3f;
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
            if(_itemStatus == 4 && _itemTimer == _playTime + 3)
            {
                _speed = ((double)DeltaTime * 20) *_speedIncrItem * _fasterSpeedIncr;
            }
            else if(_itemStatus == 0)
            {
                _speed = ((double)DeltaTime * 20) * _speedIncrItem * _fasterSpeedIncr;
            }

            //Bounding Boxes and collision detection

            //Boundind Box des Schiffs  
            _shipBox = _starshipTrans.Matrix() * _starShipMesh.BoundingBox;
            //Console.WriteLine(_shipBox.max); //(4,339855; 1,9487817; 1,7474995)     (1,3398551; 1,9487817; 1,7474995)
            //Console.WriteLine(_shipBox.min); //(1,6601449; 1,120019; -1,2371582)    (-1,3398551; 1,120019; -1,2371582)    

            if (_start)
            {
                _playTime = StartTimer(_appStartTime);

                //Hier werden für Trenches sowie ihre jeweiligen obstacles Listen erstellt, die einzeln abgegangen werden, um nach einer Kollision zu prüfen
                for (int j = 0; j < TrenchParent.Children.Count; j++)
                {
                    var _trenchTrans = TrenchParent.Children.ElementAt(j).GetTransform();
                    _trenchTrans.Translation.z -= (float)_speed;         //Die Bewegung der Szene wird aktiviert


                    if (_trenchTrans != null)
                    {
                        List<SceneNode> ObstaclesList = TrenchParent.Children.ElementAt(j).Children.FindNodes(node => node.Name.Contains("Car")).ToList();
                        for(int q = 0; q < ObstaclesList.Count(); q++)
                        {
                            if (ObstaclesList[q].Name.Contains("Material"))
                            {
                                ObstaclesList.RemoveAt(q);
                                q -= 1;
                            }
                        }
                        
                        
                        List<SceneNode> ItemList = TrenchParent.Children.ElementAt(j).Children.FindNodes(node => node.Name.Contains("ItemOrb")).ToList();


                        if (_itemStatus != 1) //Wenn Invincvibility nicht aktiv ist
                        {

                            for (int i = 0; i < ObstaclesList.Count(); i++)
                            {
                                _cubeObstTrans = TrenchParent.Children.ElementAt(j).Children.FindNodes(node => node.Name == "Car" + i)?.FirstOrDefault()?.GetTransform();

                                _cubeObstMesh = TrenchParent.Children.ElementAt(j).Children.FindNodes(node => node.Name == "Car" + i)?.FirstOrDefault()?.GetMesh();

                                AABBf cubeHitbox = _trenchTrans.Matrix() * _cubeObstTrans.Matrix() * _cubeObstMesh.BoundingBox;

                                if (_itemStatus == 5 && _itemTimer >= _playTime + 2)
                                {
                                    _laserHitbox = _laserTrans.Matrix() * _laserBB;
                                    //Console.WriteLine(_laserHitbox.max); //(0, 29645815; 26,065257; 0,2964581)        (-2, 6986864; 12,065257; 0,2964581)                 (3, 0589507; 12,065257; 0,2964581)
                                    //Console.WriteLine(_laserHitbox.min); //(-0,304214; -23,796085; -0,29999983)         (-3, 2916024; -12,657428; -0,2964581)           (2, 4660347; -12,657428; -0,2964581)
                                    //Console.WriteLine(_laserHitbox.Center); //(0; -0,29608536; 0)
                                    //Console.WriteLine(_laserTrans.Translation); //(0,004635272; 1,207751; 0)
                                    //Console.WriteLine(_laserMesh.BoundingBox.Size); //(0,59999967; 50; 0,59999967)

                                    LaserCollision(_laserHitbox, cubeHitbox);

                                    Collision(_shipBox, cubeHitbox);
                                }

                                else if (_itemStatus == 2) //Wenn Shield aktiv ist
                                {
                                    ShieldCollision(_shipBox, cubeHitbox);
                                }
                                else
                                {

                                    if(_itemTimer <= _playTime - 1)
                                    {
                                        _cubeObstMesh.Active = true;
                                        _cubeObstMesh.BoundingBox.min.y = -1;
                                    }
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
                _speed = 0;
            }

            if (_counterLR > 0)        //Bewegung und Tilt
            {
                _starshipTrans.Translation = float3.Lerp(_newPos, _oldPos, M.SmootherStep((_counterLR) / 0.3f));
                if (_newPos.x < _oldPos.x)
                {
                    _starshipTrans.Rotation.y = M.SmootherStep(_counterLR / 0.3f) * -0.167f;
                }
                else if (_newPos.x > _oldPos.x)
                {
                    _starshipTrans.Rotation.y = M.SmootherStep(_counterLR / 0.3f) * 0.167f;
                }

                _counterLR -= DeltaTime;
            }
            else if (_counterLR < 0)
            {
                _counterLR = 0;
                _starshipTrans.Rotation.y = 0;
            }

            float3 newPosXY = new float3(_starshipTrans.Translation.x, _newPosY, _starshipTrans.Translation.z);
            float3 oldPosXY = new float3(_starshipTrans.Translation.x, _oldPosY, _starshipTrans.Translation.z);

            if (_counterUD > 0)        //Bewegung und Tilt Oben/Unten
            {
                _starshipTrans.Translation = float3.Lerp(newPosXY, oldPosXY, M.SmootherStep((_counterUD) / 0.3f));
                _newPos.y = _newPosY;
                _oldPos.y = _newPosY;
                Console.WriteLine("NewPosY: " + _newPosY);
                if (_newPosY < _oldPosY)
                {
                    _starshipTrans.Rotation.x = M.SmootherStep(_counterUD / 0.3f) * 1 - M.PiOver2;//0.167f;
                    Console.WriteLine("RotUp: " + _starshipTrans.Rotation.x);
                }
                else if (_newPosY > _oldPosY)
                {
                    _starshipTrans.Rotation.x = M.SmootherStep(_counterUD / 0.3f) * -1 - M.PiOver2; // 0.167f;
                    Console.WriteLine("RotDown: " + _starshipTrans.Rotation.x);
                }

                _counterUD -= DeltaTime;

            }
            else if (_counterUD < 0 && _counterUD > -0.2f)
            {
                _counterUD -= DeltaTime;
            }
            else if (_counterUD < -0.2f && _counterUD >= -0.5f)
            {
                _starshipTrans.Translation = float3.Lerp(newPosXY, oldPosXY, M.SmootherStep((_counterUD + 0.2f) / -0.3f));

                if (_oldPosY < _newPosY)
                {
                    _starshipTrans.Rotation.x = M.SmoothStep(-(_counterUD + 0.2f) / 0.3f) * 1.57f -M.PiOver2;// 1.5f; //-0.167f;
                }
                else if (_oldPosY > _newPosY)
                {
                    _starshipTrans.Rotation.x = M.SmoothStep(-(_counterUD + 0.2f) / 0.3f) * -0.7f - M.PiOver2; // 1.5f;//0.167f;
                }
                _newPos.y = _oldPosY;
                _oldPos.y = _newPosY;

                _counterUD -= DeltaTime;
            }
            else if (_counterUD < -0.5f)
            {
                _newPosY = _oldPosY;
                _counterUD = 0;
                _starshipTrans.Rotation.x = -M.PiOver2;
                //_starshipTrans.Rotation.z = M.PiOver2;
            }


            RC.View = float4x4.LookAt(0, 8, -8, 0, 0, 7, 0, 1, 0);



            //Tick any animations and Render the scene loaded in Init()

            var orthographic = float4x4.CreateOrthographic(Width, Height, ZNear, ZFar);

            _sceneRenderer.Render(RC);

            RC.Projection = orthographic;

            //Console.WriteLine(playTime.ToString());


            _timerText.Text = _playTime.ToString();
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


        private SceneContainer CreateUIStart() //UI für Start mit Enter
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
            _start = true;
            _appStartTime = RealTimeSinceStart;
            _speed = (double)DeltaTime * 20;
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
            TrenchParent.Children.Remove(_currentTrench);
            TrenchParent.Children.Remove(_newTrench);

            _currentTrench = CopyNode(_trenchesList[0]);
            _newTrench = CopyNode(_trenchesList[_random.Next(0, _trenchCount)]);

            _newTrench.GetTransform().Translation.z += _newTrench.GetTransform().Scale.z;


            TrenchParent.Children.Add(_currentTrench);
            TrenchParent.Children.Add(_newTrench);


            _currentTrenchTrans = _currentTrench.GetTransform().Translation.z;
            _itemStatus = 0;
            _cubeObstMesh.Active = true;
            _cubeObstMesh.BoundingBox.min.y = -1;
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
                _itemTimer = _playTime + 0.2f;
            }
        }

        private void LaserCollision(AABBf _laserHitbox, AABBf cubeHitbox)
        {
            if (_laserHitbox.Intersects(cubeHitbox))
            {
                _cubeObstMesh.Active = false;
                //_cubeObstMesh.BoundingBox.min.y = 7;
                Console.WriteLine("Pew Pew!");
            }
        }

        private void ObtainItem(AABBf _shipBox, AABBf itemHitbox)
        {
            if (itemHitbox.Intersects(_shipBox.Center))
            {               
                _itemOrbMesh.Active = false;
                _itemOrbMesh.BoundingBox.min.y = 8;
                _random = new Random();
                _itemStatus = _random.Next(1, 6);    //hier random item 1-x
                if(_itemStatus != 2 && _itemStatus != 3)
                {
                    _itemTimer = _playTime + 3;//(float)speed * 60 / playTime;
                }
                else if (_itemStatus == 2)
                {
                    _itemTimer = _playTime + 20;    //lange Zeit für Schild, Timer wird runtergesetzt bei Kollision
                }
                else if (_itemStatus == 3)
                {
                    _itemTimer = _playTime + 1;
                }
                
                Console.WriteLine(_itemTimer);
            }
        }


        private void Death()
        {
            currentScore = _playTime;
            Leaderboard();
            _start = false;
            status = 2;
            _ldrbrdText.Text = "Leaderboard";
            for (int m = 0; m < 10; m++)
            {
                _ldrbrdText.Text += "\n" +  Math.Round((ScoresList[m].topTime), 3).ToString();
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