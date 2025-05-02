using System;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input.Custom;
using OpenTK.Graphics.OpenGL4;
using BoxClipping.OpenGL;
using BoxClipping.OpenTK;
using System.IO;
using Rhino.Render.ChangeQueue;


namespace BoxClipping
{
    public class BoxClippingCommand : Command
    {
        public BoxClippingCommand()
        {
            Instance = this;
        }

        public static BoxClippingCommand Instance { get; private set; }

        public override string EnglishName => "BoxClippingCommand";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            //var go = new GetObject();
            //go.SetCommandPrompt("Select a box (Brep or Extrusion)");
            //go.GeometryFilter = ObjectType.Brep | ObjectType.Extrusion;
            //go.SubObjectSelect = false;
            //go.Get();
            //if (go.CommandResult() != Result.Success)
            //    return go.CommandResult();

            //var objRef = go.Object(0);
            //var geometry = objRef.Geometry();

            //Brep brep = null;

            //if (geometry is Brep b)
            //    brep = b;
            //else if (geometry is Extrusion extrusion)
            //    brep = extrusion.ToBrep();

            //if (brep == null || !brep.IsBox())
            //{
            //    RhinoApp.WriteLine("Selected object is not a valid box.");
            //    return Result.Failure;
            //}

            //var id = objRef.ObjectId;

            var conduit = new BoxClippingConduit();
            conduit.Enabled = true;
            doc.Views.Redraw();

            return Result.Success;
        }





        public class BoxClippingConduit : DisplayConduit
        {
            private readonly Guid _boxId;
            private bool _glLoaded = false;
            private int _program;
            private int _vbo;
            private int _vao;
            private int _uBoxMinLocation;
            private int _uBoxMaxLocation;
            private DateTime _startTime = DateTime.Now;



            //public BoxClippingConduit(Guid boxId)
            //{
            //    _boxId = boxId;
            //}

            protected override void DrawForeground(DrawEventArgs e)
            {

                if (!DisplayPipeline.MakeDefaultOpenGLContextCurrent())
                {
                    RhinoApp.WriteLine("❌ GL context not available.");
                    return;
                }
                //var rhObj = RhinoDoc.ActiveDoc.Objects.Find(_boxId);
                //if (rhObj == null)
                //    return;

                //GeometryBase geometry = rhObj.Geometry;
                //Brep brep = null;

                //if (geometry is Brep b)
                //    brep = b;
                //else if (geometry is Extrusion extrusion)
                //    brep = extrusion.ToBrep();

                //if (brep == null || !brep.IsBox())
                //{
                //    RhinoApp.WriteLine("The object is no longer a valid box.");
                //    return;
                //}

                //var bbox = brep.GetBoundingBox(true);
                //var boxMin = bbox.Min;
                //var boxMax = bbox.Max;

                //RhinoApp.WriteLine($"Box bounds: {boxMin} ~ {boxMax}");

                // OpenTKにバインディングさせる（最初の1回だけ）
                if (!_glLoaded)
                {
                    GL.LoadBindings(new RhinoOpenTKBindings());

                    // シェーダープログラムをロード
                    string baseDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    string shaderDir = Path.Combine(baseDir, "shaders");

                    string vertexPath = Path.Combine(shaderDir, "vertex.glsl");
                    string fragmentPath = Path.Combine(shaderDir, "fragment.glsl");

                    _program = ShaderLoader.LoadShaderProgram(vertexPath, fragmentPath);
                    _uBoxMinLocation = GL.GetUniformLocation(_program, "uBoxMin");
                    _uBoxMaxLocation = GL.GetUniformLocation(_program, "uBoxMax");


                    if (_uBoxMinLocation == -1 || _uBoxMaxLocation == -1)
                    {
                        RhinoApp.WriteLine("⚠️ Uniform location not found.");
                    }

                    //頂点
                    float[] vertices = new float[]
                    {
                        -1f, -1f,  // 左下
                         1f, -1f,  // 右下
                         1f,  1f,  // 右上
                        -1f,  1f   // 左上
                    };

                    uint[] indices = { 0, 1, 2, 2, 3, 0 };


                    GL.GenVertexArrays(1, out _vao);
                    GL.BindVertexArray(_vao);

                    GL.GenBuffers(1, out _vbo);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
                    GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

                    GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
                    GL.EnableVertexAttribArray(0);

                    _glLoaded = true;
                }
                GL.UseProgram(_program);

                float time = (float)(DateTime.Now - _startTime).TotalSeconds;

                int timeLocation = GL.GetUniformLocation(_program, "uTime");
                if (timeLocation != -1)
                    GL.Uniform1(timeLocation, time);

                var uResLocation = GL.GetUniformLocation(_program, "uResolution");
                if (uResLocation != -1)
                {
                    var viewportSize = e.Viewport.Size;
                    GL.Uniform2(uResLocation, (float)viewportSize.Width, (float)viewportSize.Height);
                }
                else
                {
                    RhinoApp.WriteLine("⚠️ uResolution uniform not found.");
                }


                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                GL.BindVertexArray(_vao);
                GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);

                // オプション：表示用のボックスを描く
                //e.Display.DrawBox(new Box(bbox), System.Drawing.Color.Red);
            }
        }
    }
}
