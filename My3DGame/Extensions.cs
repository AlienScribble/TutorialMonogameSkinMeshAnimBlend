using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace My3DGame {
    static class Extensions {
        // Allows you to store index of a mesh by name ahead of time (during initialize or load)
        public static int GetIndex(this Model model, string mesh_name)
        {
            int index = 0;
            foreach (ModelMesh mesh in model.Meshes) {
                foreach (BasicEffect effect in mesh.Effects) {
                    if (mesh.Name == mesh_name) return index;
                    index++;
                }
            }
            Console.WriteLine("Mesh named:  " + mesh_name + "  was not found.");
            return 0;
        }



        // FROM POINT OF VIEW OF SOMEONE LOOKING DOWN ON THE MAP (ROTATE PLAYER TO FACE ANGLE OF MOVEMENT): 
        public static float ToAngleFlipZ(this Vector2 vec)
        {
            return (float)Math.Atan2(-vec.Y, vec.X);
        }
    }
}
