// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.Numerics
open System.Runtime.CompilerServices
open Veldrid
open Veldrid.Sdl2
open Veldrid.StartupUtilities
open Veldrid.SPIRV
open System.Text
open Velf.Types

// Define a function to construct a message to print
let from whom =
    sprintf "from %s" whom


type PipelineData = {
    mutable GraphicsDevice : GraphicsDevice ;
    mutable CommandList : CommandList ;
    mutable VertexBuffer : DeviceBuffer ;
    mutable IndexBuffer : DeviceBuffer ;
    mutable Shaders : Shader[] ;
    mutable Pipeline : Pipeline ;
}

type VertexPositionColor =
    struct
        val SizeInBytes : uint 
        val Position : Vector2
        val Color : RgbaFloat
        new (position : Vector2, color: RgbaFloat) = { SizeInBytes = uint 24; Position = position; Color = color; }
    end



let CreateResources (pipe : PipelineData) (shads : ShaderStore) =
            let factory = pipe.GraphicsDevice.ResourceFactory;
            let mutable quadVertices =
                [|
                    VertexPositionColor(Vector2(-0.75f, 0.75f),RgbaFloat.Red);
                    VertexPositionColor(Vector2(0.75f, 0.75f), RgbaFloat.Green);
                    VertexPositionColor(Vector2(-0.75f, -0.75f), RgbaFloat.Blue);
                    VertexPositionColor(Vector2(0.75f, -0.75f), RgbaFloat.Yellow)
                |]
                
            let vbDescription = 
                BufferDescription(
                    uint 4 * uint 32,
                    BufferUsage.VertexBuffer
                )
            pipe.VertexBuffer <- factory.CreateBuffer(vbDescription)
            pipe.GraphicsDevice.UpdateBuffer(pipe.VertexBuffer, uint32 0, quadVertices) |> ignore

            let quadIndices = [|0; 1; 2; 3|]
            let ibDescription = 
                BufferDescription(
                    uint32 4 * uint32 16,
                    BufferUsage.IndexBuffer
                )
            pipe.IndexBuffer <- factory.CreateBuffer(ibDescription)
            pipe.GraphicsDevice.UpdateBuffer(pipe.IndexBuffer, uint32 0, quadIndices)

            let vertexLayout =
                VertexLayoutDescription(
                    elements = [|
                        VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2);
                        VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4)
                    |]
                )

            let vertexShaderDesc = 
                ShaderDescription(
                    ShaderStages.Vertex,
                    shads.VertexShaders.["myshader"],
                    "main"
                )
            let fragmentShaderDesc =
                ShaderDescription(
                    ShaderStages.Fragment,
                    shads.FragmentShaders.["myshader"],
                    "main"
                )

            pipe.Shaders <- factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc)

            // Create pipeline
            let mutable pipelineDescription = GraphicsPipelineDescription();
            pipelineDescription.BlendState <- BlendStateDescription.SingleOverrideBlend;
            pipelineDescription.DepthStencilState <- 
                DepthStencilStateDescription(
                    depthTestEnabled = true,
                    depthWriteEnabled = true,
                    comparisonKind = ComparisonKind.LessEqual
                )
            pipelineDescription.RasterizerState <-
                RasterizerStateDescription(
                    cullMode = FaceCullMode.Back,
                    fillMode = PolygonFillMode.Solid,
                    frontFace = FrontFace.Clockwise,
                    depthClipEnabled = true,
                    scissorTestEnabled = false
                )
            pipelineDescription.PrimitiveTopology <- PrimitiveTopology.TriangleStrip
            pipelineDescription.ResourceLayouts <- System.Array.Empty<ResourceLayout>()
            pipelineDescription.ShaderSet <- ShaderSetDescription(
                vertexLayouts = [|vertexLayout|],
                shaders = pipe.Shaders)
            pipelineDescription.Outputs <- pipe.GraphicsDevice.SwapchainFramebuffer.OutputDescription

            pipe.Pipeline <- factory.CreateGraphicsPipeline(pipelineDescription);

            pipe.CommandList <- factory.CreateCommandList()
        

let Draw (pipe : PipelineData) =
    pipe.CommandList.Begin()

    // We want to render directly to the output window.
    pipe.CommandList.SetFramebuffer(pipe.GraphicsDevice.SwapchainFramebuffer)
    pipe.CommandList.ClearColorTarget(uint32 0, RgbaFloat.Black)

    // Set all relevant state to draw our quad.
    pipe.CommandList.SetVertexBuffer(uint32 0, pipe.VertexBuffer)
    pipe.CommandList.SetIndexBuffer(pipe.IndexBuffer, IndexFormat.UInt16)
    pipe.CommandList.SetPipeline(pipe.Pipeline)
    // Issue a Draw command for a single instance with 4 indices.
    pipe.CommandList.DrawIndexed(
        indexCount = uint32 4,
        instanceCount = uint32 1,
        indexStart = uint32 0,
        vertexOffset = 0,
        instanceStart = uint32 0)

    // End() must be called before commands can be submitted for execution.
    pipe.CommandList.End()
    pipe.GraphicsDevice.SubmitCommands(pipe.CommandList)

    // Once commands have been submitted, the rendered image can be presented to the application window.
    pipe.GraphicsDevice.SwapBuffers()

let DisposeResources (pipe : PipelineData) =
    pipe.Pipeline.Dispose();
    for shader in pipe.Shaders
        do
            shader.Dispose()
    pipe.CommandList.Dispose();
    pipe.IndexBuffer.Dispose();
    pipe.GraphicsDevice.Dispose();

let runWindow (name : string) = 
    let windowCI = 
        WindowCreateInfo(
            X = 100,
            Y = 100,
            WindowWidth = 960,
            WindowHeight = 540,
            WindowTitle = name
        )

    let window = VeldridStartup.CreateWindow(ref windowCI)
    let pipe = {
            GraphicsDevice = null;
            CommandList = null ;
            VertexBuffer = null ;
            IndexBuffer = null ;
            Shaders = null ;
            Pipeline = null
        }
    pipe.GraphicsDevice <- VeldridStartup.CreateGraphicsDevice(window)
    let store = createShaders "G:\\dotnetProj\\velf\\Shaders"

    (pipe, store)
        ||> CreateResources

    while window.Exists
        do
        window.PumpEvents() |> ignore

        if window.Exists
        then
            Draw pipe
        
    

    DisposeResources pipe
    

[<EntryPoint>]
let main argv =
    runWindow "Hello World"
    0 // return an integer exit code