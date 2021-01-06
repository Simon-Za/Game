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



        private float appStartTime;             //Die Zeit seit Start der Applikation
        private float playTime;                 //Die Zeit seit drücken des Startknopfs (Leertaste)             wird später benutzt für das Leaderboard/aktive Anzeige ingame(?)
        private bool start;
        private double speed;
        private bool d;


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

        private SceneNode _currentItem;


        List<SceneNode> TrenchesList;

        List<SceneNode> ItemList;


        private readonly CanvasRenderMode _canvasRenderMode = CanvasRenderMode.Screen;
        private GUIButton _btnStart;
        private float2 _btnStartPosition;
        private SceneContainer _uiStart;
        private SceneInteractionHandler _sihS;
        private SceneRendererForward _uiStartRenderer;

        private SceneContainer _uiGame;
        private SceneRendererForward _uiGameRenderer;
        private SceneInteractionHandler _sihG;

        private GUIText _timerText;

        private GUIButton _btnRetry;
        private float2 _btnRetryPosition;
        private SceneContainer _uiDeath;
        private SceneRendererForward _uiDeathRenderer;
        private SceneInteractionHandler _sihD;

        private const float ZNear = 1f;
        private const float ZFar = 1000;


        //private enum Status {Start, Game, Death};
        private int status = 0;

        List<TextNode> ScoresList;

        private double currentScore;

        private string path = @"c:\temp\Leaderboard.txt";

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


            //ScoresList = new List<TextNode>
            {
            };

            _color = (float4)_starshipScene.Children.FindNodes(node => node.Name == "Ship")?.FirstOrDefault()?.GetComponent<DefaultSurfaceEffect>().GetFxParam<float4>("SurfaceInput.Albedo");


            //Funktionsname();
            //Leaderboard();
        }

        private void Funktionsname()
        {
            var blub = new Leaderboard();
            blub.Scores = new List<Score>
            {
                new Score(0.000), new Score(0.000), new Score(0.000), new Score(0.000), new Score(0.000), new Score(0.000), new Score(0.000), new Score(0.000), new Score(0.000), new Score(0.000)
            };

            var ser = new XmlSerializer(typeof(Leaderboard));
            using StringWriter TextWriter = new StringWriter();
            ser.Serialize(TextWriter, blub);
            File.WriteAllText("Leaderboard.xml", TextWriter.ToString());
            TextWriter.Dispose();

            FileStream fs = new FileStream("Leaderboard.xml", System.IO.FileMode.OpenOrCreate);
            TextReader reader = new StreamReader(fs);

            ser.Deserialize(reader);

            for (int k = 0; k < blub.Scores.Count(); k++)
            {
                if (currentScore >= blub.Scores.ElementAt(k).topTime)
                {
                    blub.Scores.Insert(k, new Score(currentScore));
                }
            }
        }




        public override void RenderAFrame()
        {

            // Clear the backbuffer
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);

            RC.Viewport(0, 0, Width, Height);


            //Item Shenanigans
            if (_itemTimer <= playTime + _itemTimer && _itemOrbMesh != null)
            {
                _itemOrbMesh.Active = true;
                //_itemStatus = 0;
            }
            if (_itemTimer <= playTime && _itemOrbMesh != null)
            {
                _itemStatus = 0;
             
            }

            if (_itemStatus == 1)
            {
                _starshipScene.Children.FindNodes(node => node.Name == "Ship")?.FirstOrDefault()?.GetComponent<DefaultSurfaceEffect>().SetFxParam("SurfaceInput.Albedo", new float4(3, 3, 0, 1));
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
                //if (up)
                //{
                //    //do nothing
                //    normal = false;
                //}
                //else if (normal)
                //{
                //_starshipTrans.Translation.y += 2.5f;
                oldPosY = newPosY;
                newPosY = 4.2039146f;
                counterUD = 0.3f;

                //    normal = false;
                //    up = true;

                //}
                //else if (down)
                //{
                //    //_starshipTrans.Translation.y += 2.5f;
                //    oldPosY = newPosY;
                //    newPosY = 1.7039146f;
                //    counterUD = 0.3f;

                //    down = false;
                //    normal = true;
                //}
                //_starshipTrans.Rotation.z = + 0.083f;
            }
            if (Keyboard.IsKeyDown(KeyCodes.Down) && counterUD == 0 || Keyboard.IsKeyDown(KeyCodes.S) && counterUD == 0)
            {
                //if (up)
                //{
                //_starshipTrans.Translation.y -= 2.5f;
                //oldPosY = newPosY;
                //newPosY = 1.7039146f;
                //counterUD = 0.3f;

                //up = false;
                //normal = true;
                //}
                //else if (normal)
                //{
                //_starshipTrans.Translation.y -= 2.5f;
                oldPosY = newPosY;
                newPosY = -0.7960852f;
                counterUD = 0.3f;

                //normal = false;
                //down = true;
                //}
                //else if (down)
                //{
                //    //do nothing
                //    normal = false;
                //}
                //_starshipTrans.Rotation.z = -0.083f;
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
                        List<SceneNode> ObstaclesList = TrenchParent.Children.ElementAt(j).Children.FindNodes(node => node.Name.Contains("CubeObstacle")).ToList(); //könnte statt TrenchParent.Children wahrscheinlich einfach currentTrench nehmen
                        
                        List<SceneNode> ItemList = TrenchParent.Children.ElementAt(j).Children.FindNodes(node => node.Name.Contains("ItemOrb")).ToList();


                        if (_itemStatus != 1) //Wenn Invinvibility nicht aktiv ist
                        {
                            for (int i = 0; i < ObstaclesList.Count(); i++)
                            {
                                _cubeObstTrans = TrenchParent.Children.ElementAt(j).Children.FindNodes(node => node.Name == "CubeObstacle" + i)?.FirstOrDefault()?.GetTransform();

                                _cubeObstMesh = TrenchParent.Children.ElementAt(j).Children.FindNodes(node => node.Name == "CubeObstacle" + i)?.FirstOrDefault()?.GetMesh();

                                AABBf cubeHitbox = _trenchTrans.Matrix() * _cubeObstTrans.Matrix() * _cubeObstMesh.BoundingBox;

                                Collision(_shipBox, cubeHitbox);
                            }
                        }

                        for (int i = 0; i < ItemList.Count(); i++)
                        {

                            _currentItem = TrenchParent.Children.ElementAt(j).Children.FindNodes(node => node.Name == "ItemOrb" + i)?.FirstOrDefault();
                            _itemOrbTrans = TrenchParent.Children.ElementAt(j).Children.FindNodes(node => node.Name == "ItemOrb" + i)?.FirstOrDefault()?.GetTransform();
                            _itemOrbMesh = TrenchParent.Children.ElementAt(j).Children.FindNodes(node => node.Name == "ItemOrb" + i)?.FirstOrDefault()?.GetMesh();

                            AABBf itemHitbox = _trenchTrans.Matrix() * _itemOrbTrans.Matrix() * _itemOrbMesh.BoundingBox;

                            ObtainItem(_shipBox, itemHitbox);
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
                _uiDeathRenderer.Render(RC);
                _sihD.CheckForInteractiveObjects(RC, Mouse.Position, Width, Height);
            }

            // Swap buffers: Show the contents of the backbuffer (containing the currently rendered frame) on the front buffer.
            Present();

        }




        //private SceneContainer CreateUIStart()   //UI für Start mit Button
        //{
        //    var vsTex = AssetStorage.Get<string>("texture.vert");
        //    var psTex = AssetStorage.Get<string>("texture.frag");
        //    var vsNineSlice = AssetStorage.Get<string>("nineSlice.vert");
        //    var psNineSlice = AssetStorage.Get<string>("nineSliceTile.frag");

        //    var canvasWidth = Width / 100f;
        //    var canvasHeight = Height / 100f;

        //    _btnStart = new GUIButton
        //    {
        //        Name = "Start_Button"
        //    };
        //    _btnStart.OnMouseDown += StartGame;
        //    _btnStartPosition = new float2(canvasWidth / 2 - 1f, canvasHeight / 3);

        //    var startNode = new TextureNode(
        //    "StartButtonLogo",
        //    vsTex,
        //    psTex,
        //    new Texture(AssetStorage.Get<ImageData>("StartButton.png")),
        //    UIElementPosition.GetAnchors(AnchorPos.DownDownRight),
        //    UIElementPosition.CalcOffsets(AnchorPos.DownDownRight, _btnStartPosition, canvasHeight, canvasWidth, new float2(1.6f, 0.6f)),
        //    float2.One
        //    );
        //    //var startNode = new TextureNode(
        //    //    "StartButtonLogo",
        //    //    vsNineSlice,
        //    //    psNineSlice,
        //    //    new Texture(AssetStorage.Get<ImageData>("StartButton.png")),
        //    //    UIElementPosition.GetAnchors(AnchorPos.DownDownRight),
        //    //    UIElementPosition.CalcOffsets(AnchorPos.DownDownRight, _btnStartPosition, canvasHeight, canvasWidth, new float2(1.6f, 0.6f)),
        //    //    float2.One,
        //    //    new float4(1, 1, 1, 1),
        //    //    0, 0, 0, 0
        //    //    );
        //    startNode.Components.Add(_btnStart);


        //    var canvas = new CanvasNode(
        //        "Canvas",
        //        _canvasRenderMode,
        //        new MinMaxRect
        //        {
        //            Min = new float2(-canvasWidth / 2, -canvasHeight / 2f),
        //            Max = new float2(canvasWidth / 2, canvasHeight / 2f)
        //        })
        //    {
        //        Children = new ChildList()
        //        {
        //            //Simple Texture Node, contains the fusee logo. Lüge
        //            startNode

        //        }
        //    };


        //    return new SceneContainer
        //    {
        //        Children = new List<SceneNode>
        //        {
        //            //Add canvas.
        //            canvas
        //        }
        //    };

        //}

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
                ColorUint.Tofloat4(ColorUint.White),
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


        //private SceneContainer CreateUIDeath() //UI für Todesscreen
        //{
        //    var vsTex = AssetStorage.Get<string>("texture.vert");
        //    var psTex = AssetStorage.Get<string>("texture.frag");
        //    var vsNineSlice = AssetStorage.Get<string>("nineSlice.vert");
        //    var psNineSlice = AssetStorage.Get<string>("nineSliceTile.frag");

        //    var canvasWidth = Width / 100f;
        //    var canvasHeight = Height / 100f;

        //    _btnRetry = new GUIButton
        //    {
        //        Name = "Retry_Button"
        //    };
        //    _btnRetry.OnMouseDown += TryAgain;
        //    _btnRetryPosition = new float2(canvasWidth / 2 - 1f, canvasHeight / 3);

        //    var retryNode = new TextureNode(
        //    "StartButtonLogo",
        //    vsTex,
        //    psTex,
        //    new Texture(AssetStorage.Get<ImageData>("tryAgainBig.png")),
        //    UIElementPosition.GetAnchors(AnchorPos.DownDownRight),
        //    UIElementPosition.CalcOffsets(AnchorPos.DownDownRight, _btnRetryPosition, canvasHeight, canvasWidth, new float2(1.6f, 0.6f)),
        //    float2.One
        //    );
        //    retryNode.Components.Add(_btnRetry);

        //    var canvas = new CanvasNode(
        //        "Canvas",
        //        _canvasRenderMode,
        //        new MinMaxRect
        //        {
        //            Min = new float2(-canvasWidth / 2, -canvasHeight / 2f),
        //            Max = new float2(canvasWidth / 2, canvasHeight / 2f)
        //        })
        //    {
        //        Children = new ChildList()
        //        {
        //            retryNode
        //        }
        //    };

        //    return new SceneContainer
        //    {
        //        Children = new List<SceneNode>
        //        {
        //            canvas
        //        }
        //    };

        //}
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


            var restartText = new TextNode(
                "Press Enter to retry",
                "RestartText",
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
                    restartText
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

        private void ObtainItem(AABBf _shipBox, AABBf itemHitbox)
        {
            if (itemHitbox.Intersects(_shipBox.Center))
            {               
                _itemOrbMesh.Active = false;
                _itemStatus = 1;
                _itemTimer = playTime + 3;//(float)speed * 60 / playTime;
                Console.WriteLine(_itemTimer);
            }
        }


        private void Death()
        {
            currentScore = playTime;
            //Leaderboard();
            start = false;
            status = 2;
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
            speed *= 1.25f; 
        }

        private void Leaderboard()

        {
            var blub = new Leaderboard();
            blub.Scores = new List<Score>
            {
                new Score(0.000), new Score(0.000), new Score(0.000), new Score(0.000), new Score(0.000), new Score(0.000), new Score(0.000), new Score(0.000), new Score(0.000), new Score(0.000)
            };

            var ser = new XmlSerializer(typeof(Leaderboard));
            using StringWriter TextWriter = new StringWriter();
            ser.Serialize(TextWriter, blub);
            File.WriteAllText("Leaderboard.xml", TextWriter.ToString());
            TextWriter.Dispose();

            //FileStream fs = new FileStream("Leaderboard.xml", System.IO.FileMode.OpenOrCreate);
            //TextReader reader = new StreamReader(fs);
            

            /*for (int k = 0; k < blub.Scores.Count(); k++)
            {
                if (currentScore >= blub.Scores.ElementAt(k).topTime)
                {
                    blub.Scores.Insert(k, new Score(currentScore));
                }
            }*/
            return(ser.Deserialize(TextWriter));

            Create a file to write to
            if (!File.Exists(path))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine(currentScore);
                }
            }
            else
            //{
            //    using (Stream sw = File.OpenWrite(path))
            //    {
            //        sw.WriteLine("Line2");
            //        File.S
            //    }
            //}

            //using (StreamWriter sw = File.CreateText(path))
            //{           
            //    sw.WriteLine("Hello");
            //    sw.WriteLine("And");
            //    sw.WriteLine("Welcome");
            //    sw.WriteLine((float)currentScore);
            //}

            // Open the file to read from.
            using (StreamReader sr = File.OpenText(path))
            {
                string s;
                while ((s = sr.ReadLine()) != null)
                {
                    //Console.WriteLine(s);
                }
            }




            /*  if(currentScore >= ScoresList[ScoresList.Count() - 1])
             {
                 ScoresList.RemoveAt[] 
                 ScoresList.Add()
             } */

        }
    }
}