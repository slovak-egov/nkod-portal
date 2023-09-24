import { useCallback, useEffect, useState } from "react";
import { CodelistValue, getCodelistItem, searchCodelistItem } from "../client";
import MultiTextBoxAutocomplete from "./MultiTextBoxAutocomplete";

type Props = 
{
    codelistId: string;
    selectedValues: string[];
    onChange: (items: string[]) => void;
    id: string;
}

export default function CodelistMultiTextBoxAutocomplete(props: Props)
{
    const [selectedOptions, setSelectedOptions] = useState<CodelistValue[]>([]);

    const codelistId = props.codelistId;
    const getOptions = useCallback(async (query: string) => {
        const codelists = await searchCodelistItem(codelistId, query);
        const codelist = codelists.find(c => c.id === codelistId);
        if (codelist) {
            return codelist.values;
        }
        return [];
    }, [codelistId]);

    const selectedValues = props.selectedValues;

    useEffect(() => {
        setSelectedOptions([]);
    }, [codelistId])

    useEffect(() => {
        async function loadValues() {
            const newOptions: CodelistValue[] = [];
            for (const id of selectedValues) {
                const existing = selectedOptions.find(v => v.id === id);
                if (!existing) {
                    const option = await getCodelistItem(codelistId, id);
                    if (option) {
                        newOptions.push(option);
                    }
                }
            }
            if (newOptions.length > 0) {
                setSelectedOptions([...selectedOptions, ...newOptions]);
            }
        }

        loadValues();
    }, [selectedValues, selectedOptions, codelistId]);

    return <MultiTextBoxAutocomplete<CodelistValue> id={props.id} getOptions={getOptions} renderOption={v => v.label} getValue={v => v.id} selectedOptions={selectedOptions} onChange={props.onChange} />
}