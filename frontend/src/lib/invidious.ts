/** YouTube search result returned by our backend /api/search/youtube proxy */
export interface SearchResult {
  videoId: string;
  title: string;
  channel: string;
  duration: number;
  thumbnailUrl: string;
  youtubeUrl: string;
}
