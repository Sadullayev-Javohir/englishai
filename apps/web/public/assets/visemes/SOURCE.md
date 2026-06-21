# Viseme reference images

`viseme-0.jpg` .. `viseme-21.jpg` are Microsoft's official viseme reference photos, one per
Azure Speech viseme ID (0 = silence, 1-21 = the standard IPA-phoneme groupings).

- Source: https://learn.microsoft.com/en-us/azure/ai-services/speech-service/how-to-speech-synthesis-viseme
  (section "Viseme ID", `media/text-to-speech/viseme-id-{0..21}.jpg`)
- Downloaded and cached locally (not hot-linked) per docs/development-guide.md rule 12.
- Used by `apps/web/src/components/speaking/VisemeFace.tsx` to render the mouth-position
  animation for any word: the backend emits an Azure viseme id + audio-offset track for the
  word (`WordPronunciationDetailDto.visemes`), and the component cross-dissolves between these
  22 fixed reference photos to match it.
