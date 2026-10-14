import { HttpErrorResponse } from '@angular/common/http';

// The body of a 400 answer from the API: messages by field name,
// for example { "Text": ["Tag <b> is not allowed."] }
interface ValidationProblem {
  errors?: Record<string, string[]>;
}

// The messages by field from a 400 answer. Empty for any other answer.
export function fieldErrorsOf(response: HttpErrorResponse): Record<string, string[]> {
  const problem = response.error as ValidationProblem | null;
  return response.status === 400 && problem?.errors ? problem.errors : {};
}
