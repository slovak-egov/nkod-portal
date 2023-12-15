import { ReactNode, useCallback, useEffect, useState } from 'react';

import Checkbox from './Checkbox';
import IdSkModule from './IdSkModule';

type Props<T> = {
    title: ReactNode;
    searchElementTitle: string;
    items: T[];
    getLabel: (item: T) => string;
    getValue: (item: T) => string;
    selectedItems: T[];
    onSelectionChange: (items: T[]) => void;
    dataTestId: string;
};

export default function SearchFilterWithQuery<T>(props: Props<T>) {
    const [query, setQuery] = useState('');

    const queryParts = query
        .toLowerCase()
        .split(' ')
        .filter((p) => p.length > 0);
    let items: T[];
    if (queryParts.length > 0) {
        items = props.items.filter((i) => {
            const label = props.getLabel(i).toLowerCase();
            const label2 = label.normalize('NFD').replace(/[\u0300-\u036f]/g, '');
            return queryParts.every((p) => label.includes(p) || label2.includes(p));
        });
    } else {
        items = props.items;
    }

    const selectedItems = props.selectedItems;
    const selectedValues = selectedItems.map(props.getValue);
    const onSelectionChange = props.onSelectionChange;

    const changeItemSelection = useCallback(
        (item: T, checked: boolean) => {
            onSelectionChange(checked ? [...selectedItems, item] : selectedItems.filter((i) => i !== item));
        },
        [selectedItems, onSelectionChange]
    );

    return (
        <IdSkModule moduleType="idsk-search-results-filter" data-testid={props.dataTestId}>
            <div className="idsk-search-results__filter idsk-search-results-filter__filter-panel">
                <div className="idsk-search-results__link-panel idsk-search-results--expand idsk-search-results__content-type-filter">
                    <button className="idsk-search-results__link-panel-button">
                        <span className="idsk-search-results__link-panel__title">{props.title}</span>
                        <span className="idsk-search-results__link-panel--span"></span>
                    </button>
                    <div className="idsk-search-results__list">
                        <input
                            className="govuk-input idsk-search-results__search__input"
                            type="text"
                            value={query}
                            onChange={(e) => setQuery(e.target.value)}
                            title={props.searchElementTitle}
                            aria-label={props.searchElementTitle}
                        />
                        <div className="idsk-option-select-filter ">
                            <div className="govuk-form-group">
                                <div className="govuk-checkboxes govuk-checkboxes--small">
                                    {items.map((item, index) => (
                                        <Checkbox
                                            key={index}
                                            label={props.getLabel(item)}
                                            checked={selectedValues.includes(props.getValue(item))}
                                            onCheckedChange={(c) => changeItemSelection(item, c)}
                                        />
                                    ))}
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </IdSkModule>
    );
}
