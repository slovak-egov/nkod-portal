import { InputHTMLAttributes } from "react";

interface IProps extends InputHTMLAttributes<HTMLInputElement>
{
    
}

export default function BaseInput(props: IProps)
{
    return <input className="govuk-input" {...props} />;
}

BaseInput.defaultProps = {
    type: 'text'
}