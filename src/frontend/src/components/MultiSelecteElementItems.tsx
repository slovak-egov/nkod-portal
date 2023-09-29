import { ReactNode, useState } from "react";
import SelectElementItems from "./SelectElementItems";
import Button from "./Button";
import GridRow from "./GridRow";
import GridColumn from "./GridColumn";

type Props<T> = 
{
    options: T[];
    selectedOptions: T[];
    onChange: (items: string[]) => void;
    renderOption: (item: T) => ReactNode;
    getValue: (item: T) => string;
    disabled?: boolean;
    id: string;
}

type OptionalValue = {
    id: string,
    label: ReactNode
}

export default function MultiSelectElementItems<T>(props: Props<T>)
{
    const [newValue, newValueChanged] = useState<string>('');
    const { options, selectedOptions, onChange, renderOption, getValue, disabled, ...rest } = props;

    return <>
        <div>
            <SelectElementItems<OptionalValue> disabled={disabled} {...rest} options={[{id: '', label: 'zvoľte zo zoznamu'}, ...options.map(v => ({id: getValue(v), label: renderOption(v)}))]} selectedValue={newValue} onChange={newValueChanged} renderOption={v => v.label} getValue={v => v.id} />
            <Button buttonType="secondary" style={{marginLeft: '20px'}} onClick={() => {
                if (newValue !== '') {
                    const list = selectedOptions.map(v => getValue(v));
                    if (!list.includes(newValue)) {
                        onChange([...list, newValue]);
                    }
                }}} disabled={newValue === '' || disabled}>
                Pridať do zoznamu
            </Button>
        </div>
    <div>
        {selectedOptions.length > 0 ? <div className="nkod-entity-detail"><div className="nkod-entity-detail-tags govuk-clearfix" style={{marginTop: '20px'}}>
                {selectedOptions.map(o => <div key={getValue(o)} className="govuk-body nkod-entity-detail-tag" style={{cursor: 'pointer'}} onClick={() => {
                    if (!disabled) {
                        onChange(selectedOptions.map(getValue).filter(x => x !== getValue(o)));
                    }
                }}>
                    <span>
                    {renderOption(o)} <span style={{marginLeft: '10px'}}>x</span>
                    </span>
                </div>)}
            </div></div> : null}
    </div></>
}