using MGSkinnedModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System;

namespace My3DGame
{   
    public class Game1 : Game
    {
        // DISPLAY
        const int SCREENWIDTH = 1024, SCREENHEIGHT = 768;   // TARGET FORMAT        
        GraphicsDeviceManager graphics;
        GraphicsDevice        gpu;
        SpriteBatch           spriteBatch;
        SpriteFont            font;
        static public int     screenW, screenH;
        Camera                cam;

        // INPUT & UTILS
        Input inp;        

        // RECTANGLES
        Rectangle desktopRect;
        Rectangle screenRect;

        // RENDERTARGETS & TEXTURES
        RenderTarget2D MainTarget;        
        
        // MODELS
        Sky     sky;
        Model   landscape;
        AnimationPlayer idleAnimPlayer, walkAnimPlayer, runAnimPlayer;
        Matrix[]        idleTransforms, walkTransforms, runTransforms, blendTransforms;
        Model[]         hero;        
        const int       IDLE=0, WALK=1, RUN=2; // (could use enum but easier to index without casting) 
        Vector3         hero_pos = new Vector3(0, -1, 0);
        Vector3         hero_vel;
        Vector2         last_nonzero_vel;
        float           hero_angle, playspeed, speed, hero_height;
        float           ground_Y;
        Matrix          mtx_hero_rotate;
        bool            onGround;
        // 3D //Basic3DObjects basic3D;                              // ie: floor, cube

        // SOUND
        Song song;  // Put the name of your song here instead of "song_title"         
        



        // C O N S T R U C T 
        public Game1()
        {  
            int desktop_width  = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width  - 10;
            int desktop_height = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - 10;
            graphics = new GraphicsDeviceManager(this) {
                PreferredBackBufferWidth  = desktop_width,
                PreferredBackBufferHeight = desktop_height,
                IsFullScreen = false, PreferredDepthStencilFormat = DepthFormat.None,
                GraphicsProfile = GraphicsProfile.HiDef // <-- important to allow 4Megs of indices at once
            };
            Window.IsBorderless = true;
            Content.RootDirectory = "Content";            
        }


        //--------------------
        // I N I T I A L I Z E 
        //--------------------
        protected override void Initialize()
        {
            // DISPLAY
            gpu = GraphicsDevice;
            PresentationParameters pp = gpu.PresentationParameters;            
            spriteBatch = new SpriteBatch(gpu);
            MainTarget  = new RenderTarget2D(gpu, SCREENWIDTH, SCREENHEIGHT, false, pp.BackBufferFormat, DepthFormat.Depth24);
            screenW = MainTarget.Width;
            screenH = MainTarget.Height;
            desktopRect = new Rectangle(0, 0, pp.BackBufferWidth, pp.BackBufferHeight);
            screenRect  = new Rectangle(0, 0, screenW, screenH);

            // NEW 
            inp = new Input(pp, MainTarget);
            
            // INIT 3D             
            cam     = new Camera(gpu, Vector3.Down, inp); // basic3D = new Basic3DObjects(gpu, cam.up, Content);            
            sky     = new Sky(gpu, Content);            
            hero    = new Model[3];
            last_nonzero_vel = new Vector2(0f, -1f);
            hero_height      = 1.5f;                      // calculate from actual height of model if model changes
            base.Initialize();
        }


        
        //--------
        // L O A D 
        //--------
        protected override void LoadContent()
        {
            font     = Content.Load<SpriteFont>("Font");            
            
            // 3D MODEL LOADING: 
            sky.Load("sky_model");
            landscape = Content.Load<Model>("landscape");
            Content.RootDirectory = "Content/stuffy";
            hero[IDLE] = Content.Load<Model>("stuffy_idle");
            hero[WALK] = Content.Load<Model>("stuffy_walk");
            hero[RUN]  = Content.Load<Model>("stuffy_run");
            Content.RootDirectory = "Content";                  // BASIC 3D   //basic3D.AddCube(20, 20, 20,Vector3.Zero, Vector3.Zero, "test_image", null); basic3D.objex[0].pos = new Vector3(10, -20, 40);
            // LOOK UP SKINNING DATA
            SkinningData skinningDataIdle = hero[IDLE].Tag as SkinningData;   if (skinningDataIdle==null) throw new InvalidOperationException("This model does not contain a SkinningData tag.");
            idleAnimPlayer = new AnimationPlayer(skinningDataIdle);
            SkinningData skinningDataWalk = hero[WALK].Tag as SkinningData;   if (skinningDataWalk==null) throw new InvalidOperationException("This model does not contain a SkinningData tag.");
            walkAnimPlayer = new AnimationPlayer(skinningDataWalk);
            SkinningData skinningDataRun = hero[RUN].Tag as SkinningData;     if (skinningDataRun ==null) throw new InvalidOperationException("This model does not contain a SkinningData tag.");
            runAnimPlayer = new AnimationPlayer(skinningDataRun);           // Create an animation player, and start decoding an animation clip.

            int bones_count = hero[IDLE].Bones.Count; // I'm assuming since all 3 will be the same count            
            idleTransforms  = new Matrix[bones_count];
            walkTransforms  = new Matrix[bones_count];
            runTransforms   = new Matrix[bones_count];
            blendTransforms = new Matrix[bones_count];
            // COPY BONE TRANSFORMS
            hero[IDLE].CopyAbsoluteBoneTransformsTo(idleTransforms);
            hero[WALK].CopyAbsoluteBoneTransformsTo(walkTransforms);
            hero[RUN].CopyAbsoluteBoneTransformsTo(runTransforms);
            // START AN ANIMATION 
            AnimationClip idleClip = skinningDataIdle.AnimationClips["Take 001"];  // "Take 001" <- ASSUMES you exported with default settings (if used scene name it may be that instead)            
            AnimationClip walkClip = skinningDataWalk.AnimationClips["Take 001"]; 
            AnimationClip runClip  = skinningDataRun.AnimationClips["Take 001"];
            idleAnimPlayer.StartClip(idleClip);
            walkAnimPlayer.StartClip(walkClip);
            runAnimPlayer.StartClip(runClip);

            // SOUND LOADING:
            song = Content.Load<Song>("ForestShadows");
            MediaPlayer.Volume = 0.5f;
            MediaPlayer.Play(song);
        }        
        protected override void UnloadContent() {
        }




        //------------
        // U P D A T E 
        //------------
        protected override void Update(GameTime gameTime)
        {
            inp.Update();
            if (inp.back_down || inp.KeyDown(Keys.Escape)) Exit(); // change to menu for exit later
            
            // CAMERA
            //cam.MoveCamera(new Vector3(inp.gp.ThumbSticks.Left.X, inp.gp.ThumbSticks.Right.Y, inp.gp.ThumbSticks.Left.Y));
            cam.Update_Player_Cam(hero_pos);
            if (inp.KeyDown(Keys.Left))     hero_vel += new Vector3(cam.view.Left.X,     0, cam.view.Left.Z)     * 0.1f;
            if (inp.KeyDown(Keys.Right))    hero_vel += new Vector3(cam.view.Right.X,    0, cam.view.Right.Z)    * 0.1f;
            if (inp.KeyDown(Keys.Up))       hero_vel += new Vector3(cam.view.Forward.X,  0, cam.view.Forward.Z)  * 0.1f;
            if (inp.KeyDown(Keys.Down))     hero_vel += new Vector3(cam.view.Backward.X, 0, cam.view.Backward.Z) * 0.1f;
            bool jump_pressed = (inp.KeyDown(Keys.Space) | (inp.gp.IsButtonDown(Buttons.A)));
            if ((jump_pressed)&&(onGround)) { hero_vel.Y = -1.3f; onGround = false; }
            // G A M E P A D   C O N T R O L   
            float MovePad_LeftRight = 0, MovePad_UpDown = 0;
            if (inp.gp.IsConnected) {
                MovePad_LeftRight = inp.gp.ThumbSticks.Left.X;
                MovePad_UpDown    = inp.gp.ThumbSticks.Left.Y;
                if ((MovePad_UpDown < -Input.DEADZONE) || (MovePad_UpDown > Input.DEADZONE) || (MovePad_LeftRight < -Input.DEADZONE) || (MovePad_LeftRight > Input.DEADZONE)) {
                    hero_vel.X = MovePad_LeftRight * cam.view.Right.X + MovePad_UpDown * cam.view.Forward.X; // left-right_control * right_from_camera + up-down_control * forward_from_camera
                    hero_vel.Z = MovePad_LeftRight * cam.view.Right.Z + MovePad_UpDown * cam.view.Forward.Z; // use this formala along x and z motions for character movement 
                }
            }
            // COLLIDE WITH LOW-GROUND:
            ground_Y = 0.8f; // = GetGround(hero_pos);   // TO DO: make StoreGround and GetGround and MakeColliders, BroadPhase & NarrowPhase Collision Tests
            if (hero_pos.Y > ground_Y - hero_height) {
                if (!onGround) {
                    hero_pos.Y = ground_Y - hero_height;
                }
                hero_vel.Y = 0;                        // TO DO: set in_air = false; so can jump again
                onGround   = true;
            } else if (hero_vel.Y<8f) hero_vel.Y += 0.03f;
            hero_pos    += hero_vel; // moves player at a speed
            hero_vel.X  *= 0.96f;    // gradually slows down player 
            hero_vel.Z  *= 0.96f;    // gradually slows down player 
            Vector2 map_vel  = new Vector2(hero_vel.X, hero_vel.Z);
            speed            = map_vel.Length();
            if (speed > 1f)    { speed = 1f; map_vel.Normalize(); hero_vel.X = map_vel.X; hero_vel.Z = map_vel.Y; }
            if (speed > 0.01f) last_nonzero_vel = map_vel;
            hero_angle       = last_nonzero_vel.ToAngleFlipZ();
            mtx_hero_rotate  = Matrix.CreateFromYawPitchRoll(hero_angle-3.14f/2, 3.14f, 0);
            playspeed        = speed * 6f + 0.5f;
            TimeSpan elapsedTime = TimeSpan.FromSeconds(gameTime.ElapsedGameTime.TotalSeconds * playspeed);
            idleAnimPlayer.Update(elapsedTime, true, Matrix.Identity);
            walkAnimPlayer.Update(elapsedTime, true, Matrix.Identity);
            runAnimPlayer.Update(elapsedTime,  true, Matrix.Identity);
            base.Update(gameTime);
        }



        #region  S E T  3 D  S T A T E S  ----------------
        RasterizerState rs_ccw = new RasterizerState() { FillMode = FillMode.Solid, CullMode = CullMode.CullCounterClockwiseFace };
        void Set3DStates() {
            gpu.BlendState = BlendState.NonPremultiplied;
            gpu.DepthStencilState = DepthStencilState.Default;
            if (gpu.RasterizerState.CullMode == CullMode.None) gpu.RasterizerState = rs_ccw;
        }
        #endregion



        //--------
        // D R A W 
        //--------
        protected override void Draw(GameTime gameTime)
        {
            gpu.SetRenderTarget(MainTarget);            
            gpu.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.TransparentBlack, 1.0f, 0);

            // RENDER SCENE OBJECTS  
            sky.Draw(cam);
            Set3DStates();                  //basic3D.Draw(cam);
            // render models: 
            DrawModel(landscape);

           // SETUP FOR SHADOW:             
            Vector3 light_pos        = new Vector3(20, -40, 20);                       // temporary until we make our own custom light effect
            //float   light_radius = 80, light_radius_sq = light_radius * light_radius;
            Vector3 litDir           = (hero_pos - light_pos);
            //float distance_intensity = litDir.LengthSquared();
            //if (distance_intensity > light_radius_sq * 4) distance_intensity = 0;  else { distance_intensity = 1f - distance_intensity / (light_radius_sq * 4); }            
            // matrix to project shadow onto plane:
            Plane plane = new Plane(Vector3.Up, ground_Y);
            Matrix ShadowTransform = Matrix.CreateShadow(new Vector3(litDir.X, litDir.Y, litDir.Z), plane);

            // START SKINNEDMESH ANIMATION
            Matrix[] bones1;
            Matrix[] bones2;
            float percent;
            if (speed <= 0.2f) {
                bones1 = idleAnimPlayer.GetSkinTransforms();
                bones2 = walkAnimPlayer.GetSkinTransforms();
                percent = 1f / 0.2f * speed;                  // first 20%
            } else {
                bones1 = walkAnimPlayer.GetSkinTransforms();
                bones2 = runAnimPlayer.GetSkinTransforms();
                percent = 1f / 0.8f * (speed - 0.2f);         // remaining 80%
            }
            int i = 0;
            while(i<bones1.Length) {
                blendTransforms[i] = Matrix.Lerp(bones1[i], bones2[i], percent);
                i++;
            }

            // Render Skinned Mesh
            foreach (ModelMesh mesh in hero[IDLE].Meshes) {
                foreach (SkinnedEffect effect in mesh.Effects) {
                    effect.Alpha = 1f;
                    effect.EnableDefaultLighting();
                    effect.PreferPerPixelLighting = true;
                    effect.SetBoneTransforms(blendTransforms);
                    effect.World      = blendTransforms[mesh.ParentBone.Index] * mtx_hero_rotate * Matrix.CreateTranslation(hero_pos);
                    effect.View       = cam.view;
                    effect.Projection = cam.proj;                    
                    effect.SpecularColor = new Vector3(0.2f, 0.3f, 0.05f);
                    effect.SpecularPower = 128f;  
                }
                mesh.Draw();
                // DRAW SHADOW
                foreach (SkinnedEffect fct in mesh.Effects) {
                    Vector3 old_color = fct.DiffuseColor;
                    fct.DiffuseColor  = Vector3.Zero;
                    fct.World *= ShadowTransform;
                    fct.Alpha  = 0.9f;             //distance_intensity;
                    fct.DiffuseColor = old_color;
                }
                mesh.Draw();
            }


            // DRAW MAINTARGET TO BACKBUFFER
            gpu.SetRenderTarget(null);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone);
            spriteBatch.Draw(MainTarget, desktopRect, Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }


        // D R A W  M O D E L 
        void DrawModel(Model model)
        {
            //Matrix[] transforms = new Matrix[model.Bones.Count]; 
            //model.CopyAbsoluteBoneTransformsTo(transforms);      // get the model transformations
            foreach (ModelMesh mesh in model.Meshes) {
                foreach (BasicEffect effect in mesh.Effects) {
                    effect.EnableDefaultLighting();
                    effect.PreferPerPixelLighting = true;
                    effect.TextureEnabled         = true;
                    effect.View       = cam.view;
                    effect.Projection = cam.proj;                    
                    effect.AmbientLightColor = new Vector3(0.2f, 0.1f, 0.3f);
                    effect.DiffuseColor      = new Vector3(0.95f, 0.96f, 0.85f);
                    effect.FogEnabled = true;
                    effect.FogStart   = 2f;
                    effect.FogEnd     = 500f;                    
                    effect.FogColor = new Vector3(0f, 0.05f, 0.06f);
                    //effect.World = world_rotation * transforms[mesh.ParentBone.Index] * Matrix.CreateTranslation(Position);
                }
                mesh.Draw();
            }

        }
    }
}
