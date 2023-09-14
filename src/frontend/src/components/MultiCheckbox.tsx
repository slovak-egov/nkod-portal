import { HTMLAttributes } from "react";
import Checkbox from "./Checkbox";

interface IProps extends HTMLAttributes<HTMLInputElement>
{
    items: string[];
}

export default function MultiCheckbox(props: IProps)
{
    return <div className="govuk-checkboxes">
        {props.items.map((item, index) => <Checkbox label={item} key={index} {...props} checked={true} onCheckedChange={() => {}} />)}
    </div>;
}