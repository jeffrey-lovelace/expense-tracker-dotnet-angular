import { Pipe, PipeTransform } from '@angular/core';

import { Category } from '../models/expense.models';

/** Category value -> its label from GET /api/categories; falls back to the value itself. */
@Pipe({ name: 'categoryLabel' })
export class CategoryLabelPipe implements PipeTransform {
  transform(value: string, categories: readonly Category[]): string {
    return categories.find((c) => c.value === value)?.label ?? value;
  }
}
