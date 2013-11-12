#r "../tools/FAKE/tools/FakeLib.dll" // include Fake lib
open Fake 

Target "Default" (fun _ ->
    trace "jigeojgwi"
)

Target "Clean" (fun _ ->
    CleanDirs ["./deploy/Rebus"]
)

Target "BuildRebus" (fun _ ->
    trace "Building Rebus..."
)

"BuildRebus"
    ==> "Default"

"Clean"
    ==> "BuildRebus"

Run "Default"