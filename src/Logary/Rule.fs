namespace Logary

open System
open System.Text.RegularExpressions
open Logary
open Logary.Internals

/// This is the accept filter that is before the message is passed to the logger
type MessageFilter = Message -> bool

/// A rule specifies what messages a target should accept.
[<CustomEquality; CustomComparison>]
type Rule =
  { /// This is the regular expression that the 'path' must match to be loggable
    hiera         : Regex
    /// This is the level at which the target will accept log lines. It's inclusive, so
    /// anything below won't be accepted.
    level         : LogLevel
    /// This is the accept filter that is before the message is passed to the logger
    /// instance.
    messageFilter : MessageFilter }

  override x.GetHashCode () =
    hash (x.hiera.ToString(), x.level)

  override x.Equals other =
    match other with
    | null -> false
    | :? Rule as o -> (x :> IEquatable<Rule>).Equals(o)
    | _ -> false

  interface System.IEquatable<Rule> with
    member x.Equals r =
      r.hiera.ToString() = x.hiera.ToString()
      && r.level = x.level

  interface System.IComparable with
    member x.CompareTo yobj =
      match yobj with
      | :? Rule as y ->
        compare (x.hiera.ToString()) (y.hiera.ToString())
        |> thenCompare x.level y.level
      | _ -> invalidArg "yobj" "cannot compare values of different types"

  override x.ToString() =
    sprintf "Rule(hiera=%O, level=%O)"
      x.hiera x.level

/// Module for dealing with rules. Rules take care of filtering too verbose
/// log lines and measures before they are sent to the targets.
///
/// Rules compose to the maximal filter. I.e. if you have one rule for your
/// target that specifies all Message-s must be of Debug level, and then another
/// filter that specifies they have to be of Info level, then the rule of Info
/// will win, and all Debug-level messages will be filtered from the stream.
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Rule =

  // filters:
  /// A filter that accepts any input given
  let allowFilter _ = true

  /// Find all rules matching the name, from the list of rules passed.
  let matching (name : PointName) (rules : Rule list) =
    rules |> List.filter (fun r -> r.hiera.IsMatch (PointName.format name))

  /////////////////////
  // Creating rules: //
  /////////////////////

  let private allHiera = Regex(".*", RegexOptions.Compiled)

  let empty =
    { hiera         = allHiera
      messageFilter = fun _ -> true
      level         = Verbose }

  /// Sets the hiera regex field.
  [<CompiledName "SetHiera">]
  let setHiera (regex : Regex) (r : Rule) =
    { r with hiera = regex }

  /// Sets the hiera regex field from a string.
  [<CompiledName "SetHiera">]
  let setHieraString (regex : string) (r : Rule) =
    { r with hiera = Regex(regex) }

  /// Sets the rule's message filter. Remember that if you want to apply multiple
  /// message filters, you need to create a new Rule, or you'll overwrite the
  /// existing message filter.
  [<CompiledName "SetMessageFilter">]
  let setMessageFilter (mf : _ -> _) (r : Rule) =
    { r with messageFilter = mf }

  [<CompiledName "SetLevel">]
  let setLevel (l : LogLevel) (r : Rule) =
    { r with level = l }

  /// Create a new rule with the given hiera, target, accept function and min level
  /// acceptable.
  [<CompiledName "Create">]
  let create hiera messageFilter level =
    { hiera         = hiera
      messageFilter = messageFilter
      level         = level }

  /// Create a new rule with the given hiera, target, accept function and min level
  /// acceptable.
  [<CompiledName "Create">]
  let createFunc hiera level (messageFilter : Func<_, _>) =
    { hiera         = hiera
      messageFilter = fun m -> messageFilter.Invoke m
      level         = level }