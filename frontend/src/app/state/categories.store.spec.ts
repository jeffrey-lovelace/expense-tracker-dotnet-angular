import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { CategoriesApiService } from '../services/categories-api.service';
import { CategoriesStore } from './categories.store';

describe('CategoriesStore', () => {
  let api: jasmine.SpyObj<CategoriesApiService>;
  let store: CategoriesStore;
  const categories = [
    { value: 'Food', label: 'Food' },
    { value: 'Other', label: 'Other' },
  ];

  beforeEach(() => {
    api = jasmine.createSpyObj<CategoriesApiService>('CategoriesApiService', ['list']);
    TestBed.configureTestingModule({
      providers: [{ provide: CategoriesApiService, useValue: api }],
    });
    store = TestBed.inject(CategoriesStore);
  });

  it('loads the categories once', () => {
    api.list.and.returnValue(of(categories));

    store.load();
    store.load();

    expect(store.categories()).toEqual(categories);
    expect(api.list).toHaveBeenCalledTimes(1);
  });

  it('can try again after a failure', () => {
    api.list.and.returnValues(
      throwError(() => new Error('0')),
      of(categories),
    );

    store.load();
    expect(store.categories()).toEqual([]);
    store.load();

    expect(store.categories()).toEqual(categories);
  });
});
