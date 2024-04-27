import { CodelistValue } from '../client';
import { SuggestionType, SuggestionStatusCode } from '../cms';

export const suggestionTypeCodeList: CodelistValue[] = [
    {
        id: SuggestionType.SUGGESTION_FOR_PUBLISHED_DATASET,
        label: 'podnet na zverejnenie nového datasetu/distribúcie'
    },
    {
        id: SuggestionType.SUGGESTION_FOR_QUALITY_OF_PUBLISHED_DATASET,
        label: 'podnet na kvalitu dát zverejneného datasetu/distribúcie'
    },
    {
        id: SuggestionType.SUGGESTION_FOR_QUALITY_OF_METADATA,
        label: 'podnet na kvalitu metadát zverejneného datasetu/distribúcie'
    },
    {
        id: SuggestionType.SUGGESTION_OTHER,
        label: 'iný podnet'
    }
];

export const suggestionStatusList: CodelistValue[] = [
    {
        id: SuggestionStatusCode.CREATED,
        label: 'zaevidovaný'
    },
    {
        id: SuggestionStatusCode.IN_PROGRESS,
        label: 'v riešení'
    },
    {
        id: SuggestionStatusCode.RESOLVED,
        label: 'vyriešený'
    }
];
