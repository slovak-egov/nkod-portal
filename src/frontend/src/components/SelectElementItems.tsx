import { ReactNode } from "react";
import SelectElement from "./SelectElement";

type Props<T> = 
{
    options: T[];
    selectedOption: T;
    onChange: (item: T) => void;
    renderOption: (item: T) => ReactNode;
    getValue: (item: T) => string;
    id: string;
}

export default function SelectElementItems<T>(props: Props<T>)
{
    const { options, selectedOption, onChange, renderOption, getValue, ...rest } = props;

    const selectedValue = getValue(selectedOption);

    return <SelectElement onChange={e => onChange(options[e.target.selectedIndex])} value={selectedValue} {...rest}>
        {options.map(option => {
            const v = getValue(option);
            return <option key={v} value={v}>{renderOption(option)}</option>
        })}
    </SelectElement>;
}