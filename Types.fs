module Velf.Types

open System
open System.IO
open System.Text

type ShaderStore = {
        mutable VertexShaders : Map<string,byte[]>
        mutable FragmentShaders : Map<string,byte[]>
}

let newShaderStore = {VertexShaders = Map.empty; FragmentShaders = Map.empty}

let remExt (name : string) =
    name.Split(".").[0]

   
let createShaders (folderPath : string) =
    let mutable shades = newShaderStore
    for file in Directory.EnumerateFiles(folderPath, "*.vert")
        do 
            shades.VertexShaders <- 
                shades.VertexShaders.Add(
                    file.Split("\\") |> Array.rev |> Array.head |> remExt,
                    Encoding.UTF8.GetBytes(File.ReadAllLines(file) |> String.concat "\n" )
                )
    for file in Directory.EnumerateFiles(folderPath, "*.frag")
        do 
            shades.FragmentShaders <- 
                shades.VertexShaders.Add(
                    file.Split("\\") |> Array.rev |> Array.head |> remExt, 
                    Encoding.UTF8.GetBytes(File.ReadAllLines(file) |> String.concat "\n")
                )
    shades

