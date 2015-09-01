﻿open System
open System.Diagnostics
open System.IO

#if MODE_AD
open DiffSharp.AD
#else
open DiffSharp.AD.Specialized.Reverse1
#endif

let write_times (fn:string) (tf:float) (tJ:float) =
    let line1 = sprintf "%f %f\n" tf tJ
    let line2 = sprintf "tf tJ"
    let lines = [|line1; line2|]
    File.WriteAllLines(fn,lines)

#if (DO_GMM_FULL || DO_GMM_SPLIT)
let test_gmm fn_in fn_out nruns_f nruns_J replicate_point =
    let alphas, means, icf, x, wishart = gmm.read_gmm_instance (fn_in + ".txt") replicate_point
    
    let obj_stop_watch = Stopwatch.StartNew()
    let err = gmm.gmm_objective alphas means icf x wishart
    for i = 1 to nruns_f-1 do
        gmm.gmm_objective alphas means icf x wishart
    obj_stop_watch.Stop()

    let k = alphas.Length
    let d = means.[0].Length
    let n = x.Length
    
#if DO_GMM_FULL
    let gmm_objective_wrapper_ (parameters:_[]) = gmm.gmm_objective_wrapper d k parameters x wishart
    let grad_gmm_objective = grad' gmm_objective_wrapper_
    let grad_func parameters = grad_gmm_objective parameters
  #if MODE_AD
    let name = "DiffSharp"
  #else
    let name = "DiffSharp_R"
  #endif
#endif
#if DO_GMM_SPLIT
    let gmm_objective_2wrapper_ (parameters:_[]) = gmm.gmm_objective_2wrapper d k n parameters wishart
    let grad_gmm_objective2 = grad' gmm_objective_2wrapper_
    let grad_gmm_objective_split (parameters:_[]) =
        let mutable err, grad = grad_gmm_objective2 parameters
        for i = 0 to n-1 do
            let gmm_objective_1wrapper_ (parameters:_[]) = gmm.gmm_objective_1wrapper d k parameters x.[i]
            let grad_gmm_objective1 = grad' gmm_objective_1wrapper_
            let err_curr, grad_curr = grad_gmm_objective1 parameters
            err <- err + err_curr
            grad <- Array.map2 (+) grad grad_curr
        (err, grad)
    let grad_func parameters = grad_gmm_objective_split parameters
  #if MODE_R
    let name = "DiffSharp_R_split"
  #endif
#endif

    let grad_stop_watch = Stopwatch.StartNew()
    if nruns_J>0 then   
        let parameters = (gmm.vectorize alphas means icf) 
#if MODE_AD
                            |> Array.map D
#endif
        let err2, gradient = (grad_func parameters)
        for i = 1 to nruns_J-1 do
            grad_func parameters
        grad_stop_watch.Stop()
    
        gmm.write_grad (fn_out + "_J_" + name + ".txt") gradient   

    let tf = ((float obj_stop_watch.ElapsedMilliseconds) / 1000.) / (float nruns_f)
    let tJ = ((float grad_stop_watch.ElapsedMilliseconds) / 1000.) / (float nruns_J)
    write_times (fn_out + "_times_" + name + ".txt") tf tJ
#endif

#if DO_BA
let test_ba fn_in fn_out nruns_f nruns_J = 
    let cams, X, w, obs, feat = ba.read_ba_instance (fn_in + ".txt")
    let aa = ba.sqnorm X.[0]
    let obj_stop_watch = Stopwatch.StartNew()
    let err = ba.ba_objective cams X w obs feat
    for i = 1 to nruns_f-1 do
        ba.ba_objective cams X w obs feat
    obj_stop_watch.Stop()

  #if MODE_AD
    let name = "DiffSharp"
  #else
    let name = "DiffSharp_R"
  #endif

    let n = cams.Length
    let m = X.Length
    let p = obs.Length
    
    let jac_stop_watch = Stopwatch.StartNew()
    if nruns_J>0 then   
        let parameters = (ba.vectorize cams X w) 
#if MODE_AD
                            |> Array.map D
#endif
        let err2, J = (J_func parameters)
        for i = 1 to nruns_J-1 do
            J_func parameters    
        jac_stop_watch.Stop()

        ba.write_J (fn_out + "_J_" + name + ".txt") gradient   
        
    let tf = ((float obj_stop_watch.ElapsedMilliseconds) / 1000.) / (float nruns_f)
    let tJ = ((float jac_stop_watch.ElapsedMilliseconds) / 1000.) / (float nruns_J)
    write_times (fn_out + "_times_" + name + ".txt") tf tJ
#endif

[<EntryPoint>]
let main argv = 
    let dir_in = argv.[0]
    let dir_out = argv.[1]
    let fn = argv.[2]
    let nruns_f = (Int32.Parse argv.[3])
    let nruns_J = (Int32.Parse argv.[4])
    let replicate_point = 
        (argv.Length >= 6) && (argv.[5].CompareTo("-rep") = 0)

#if DO_GMM_FULL || DO_GMM_SPLIT
    test_gmm (dir_in + fn) (dir_out + fn) nruns_f nruns_J replicate_point
#endif
#if DO_BA
    test_ba (dir_in + fn) (dir_out + fn) nruns_f nruns_J
#endif

    0
