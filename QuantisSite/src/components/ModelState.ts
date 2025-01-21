export enum ModelState {
  Deleted = -2,
  Failed = -1,
  Queued = 0,
  Created = 1,
  Training = 2,
  Testing = 3,
  Publishing = 4,
  Ready = 5,
}

export default ModelState