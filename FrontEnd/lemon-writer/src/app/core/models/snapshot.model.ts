export interface Snapshot {
  id: string;
  chapterId: string;
  message: string;
  content: string;
  wordCount: number;
  createdAt: string;
  authorName?: string;
}

export interface CreateSnapshotRequest {
  message: string;
  content: string;
}

export interface SnapshotDiff {
  snapshotAId: string;
  snapshotBId: string;
  additions: number;
  deletions: number;
  htmlDiff: string;
}
