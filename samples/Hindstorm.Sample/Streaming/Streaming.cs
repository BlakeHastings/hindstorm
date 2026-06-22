using Hindstorm;

namespace Hindstorm.Sample.Streaming;

// A real-time audio pipeline, modeled on the dataflow plane rather than forced into the command /
// aggregate grammar. A microphone streams frames; a voice-activity detector and an endpointer transform
// and segment them; speech-to-text lifts a segment into text; and only there, at the translation seam,
// does a measurement become a genuine domain event the transactional domain reacts to.
//
// Everything in the "AudioIngest" pipeline is plumbing: continuous, immutable, and unable to reject. The
// Conversation context to its right is the transactional domain. The single [Translates] edge is the
// anti-corruption seam between the two planes.

// ----------------------------------------------------------------------------------------------------
// The streaming plane: processors (transform stages) and the data events they pass between them.
// AbstractionLevel places each data point in a Complex Event Processing hierarchy: frame 0 -> speech
// probability 0 -> segment 1 -> transcript 2.
// ----------------------------------------------------------------------------------------------------

[DataEvent(Pipeline = "AudioIngest", AbstractionLevel = 0)]
public sealed record AudioFrame(byte[] Pcm);

[DataEvent(Pipeline = "AudioIngest", AbstractionLevel = 0)]
public sealed record SpeechProbability(double Value);

[DataEvent(Pipeline = "AudioIngest", AbstractionLevel = 1)]
public sealed record SpeechSegment(byte[] Pcm);

[DataEvent(Pipeline = "AudioIngest", AbstractionLevel = 2)]
public sealed record Transcript(string Text);

[Processor(Pipeline = "AudioIngest")]
public sealed class Microphone
{
    [Transforms(typeof(AudioFrame))]
    [Feeds(typeof(VoiceActivityDetector))]
    public AudioFrame Capture() => new([]);
}

[Processor(Pipeline = "AudioIngest")]
public sealed class VoiceActivityDetector
{
    [Transforms(typeof(SpeechProbability))]
    [Feeds(typeof(Endpointer))]
    public SpeechProbability Score(AudioFrame frame) => new(0.0);
}

[Processor(Pipeline = "AudioIngest")]
public sealed class Endpointer
{
    [Transforms(typeof(SpeechSegment))]
    [Feeds(typeof(SpeechToText))]
    public SpeechSegment Endpoint(SpeechProbability probability) => new([]);
}

[Processor(Pipeline = "AudioIngest")]
public sealed class SpeechToText
{
    [Transforms(typeof(Transcript))]
    // The seam: a transcript (a measurement) becomes UtteranceTranscribed (a domain fact) the domain reacts to.
    [Translates(typeof(UtteranceTranscribed))]
    public Transcript Transcribe(SpeechSegment segment) => new(string.Empty);
}

// ----------------------------------------------------------------------------------------------------
// The transactional domain plane: a tiny Conversation context that reacts to the translated fact.
// ----------------------------------------------------------------------------------------------------

[DomainEvent(Context = "Conversation", AbstractionLevel = 3)]
public sealed record UtteranceTranscribed(string Text);

[Command(Context = "Conversation")]
public sealed record DispatchToAgent(string Text);

[Policy(Context = "Conversation")]
public sealed class RelevancePolicy
{
    // A genuine decision (relevant or not), so a real policy on the domain plane, not a pipeline stage.
    [ReactsTo(typeof(UtteranceTranscribed))]
    [Issues(typeof(DispatchToAgent))]
    public DispatchToAgent OnTranscribed(UtteranceTranscribed transcribed) => new(transcribed.Text);
}

[Aggregate(Context = "Conversation")]
public sealed class Conversation
{
    [Handles(typeof(DispatchToAgent))]
    [Raises(typeof(AgentResponded))]
    public AgentResponded Dispatch(DispatchToAgent command) => new(command.Text);
}

[DomainEvent(Context = "Conversation")]
public sealed record AgentResponded(string Reply);
