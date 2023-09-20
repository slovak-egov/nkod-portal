import { TextareaHTMLAttributes } from "react";

interface IProps extends TextareaHTMLAttributes<HTMLTextAreaElement>
{
    
}

export default function TextArea(props: IProps)
{
    return <textarea className="govuk-textarea" {...props} />;
}

TextArea.defaultProps = {
    rows: 5
}