import { HTMLAttributes } from "react";
import Checkbox from "./Checkbox";

interface IProps<T> extends HTMLAttributes<HTMLInputElement>
{
    options: T[];
    selectedValues: string[];
    getLabel: (item: T) => string;
    getValue: (item: T) => string;
    onCheckedChanged: (selectedValues: string[]) => void;
}

export default function MultiCheckbox<T>(props: IProps<T>)
{
    const { options, selectedValues, getLabel, getValue, onCheckedChanged, ...rest } = props;
    return <div className="govuk-checkboxes">
        {options.map((item, index) => <Checkbox label={getLabel(item)} key={index} {...rest} checked={selectedValues.includes(getValue(item))} onCheckedChange={checked => {
            if (checked) {
                onCheckedChanged([...selectedValues, getValue(item)]);
            } else {
                onCheckedChanged(selectedValues.filter(v => v !== getValue(item)));
            }
        }} />)}
    </div>;
}