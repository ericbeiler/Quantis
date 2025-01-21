import { ModelState } from './ModelState'

interface ModelSummary {
  Id: number;
  Name: string;
  Description: string;
  State: ModelState;
  QualityScore: number;
  Created: string;
  Type: string;
}

export default ModelSummary