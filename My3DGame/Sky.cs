using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace My3DGame {
    class Sky {
        GraphicsDevice   gpu;
        ContentManager   Content;
        Model            model;         
        int              cloud_index;
        float            rotate;
        Matrix           translate;  // used to adjust starting position of dome
        public Texture2D texture;

        // CONSTRUCT
        public Sky(GraphicsDevice GPU, ContentManager content) {
            gpu = GPU; Content = content;
            translate = Matrix.CreateTranslation(0, 4, 0);
        }

        // L O A D
        public void Load(string SkyModelName) {            
            model = Content.Load<Model>(SkyModelName);
            cloud_index = model.GetIndex("cloud_layer"); // ***
            rotate = 0;
        }        


        // D R A W 
        public void Draw(Camera cam)
        {            
            gpu.BlendState        = BlendState.Opaque;        // no transparency
            gpu.RasterizerState   = RasterizerState.CullNone; // no-backface culling (only facing triangles of dome will be visible at any time anyway)
            gpu.DepthStencilState = DepthStencilState.None;   // NO depth (render this dome first [not actually that large] and then layer everything else on top sorted by depth - adding depth to skybox or skydome would cause it to overlap scene) 
            gpu.SamplerStates[0]  = SamplerState.LinearWrap;  // texture-uv-wrapping
            Matrix view      = cam.view;
            view.Translation = Vector3.Zero;                  // cancels the translation part of view transformation (only rotation is kept) 

            for(int i=0; i<model.Meshes.Count; i++) {  // ***
                ModelMesh mesh = model.Meshes[i];
                foreach (BasicEffect effect in mesh.Effects) {
                    if (i == cloud_index) {
                        if (texture != null) effect.Texture = texture; // makes possible to switch sky textures from default texture image
                        gpu.BlendState = BlendState.Additive;
                        effect.World   = Matrix.CreateFromYawPitchRoll(rotate, 0, 0) * translate;
                        rotate += 0.002f;                        
                    }
                    else {
                        gpu.BlendState = BlendState.Opaque;
                        effect.World   = translate;
                    }
                    effect.LightingEnabled = false;          // ***
                    effect.View            = view;           // viewing angle of skydome
                    effect.Projection      = cam.proj;       // perspective projection                    
                    effect.TextureEnabled  = true;       // make it visible (not lighting active on this since it's sky imagery)                     
                }
                mesh.Draw();
            }
        }

    }
}
