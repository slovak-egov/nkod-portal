import React, { forwardRef, InputHTMLAttributes } from 'react';

type Props = InputHTMLAttributes<HTMLInputElement>

const BaseInput = forwardRef<HTMLInputElement, Props>((props, ref) => {
    return <input className='govuk-input' ref={ref} {...props} />;
});

BaseInput.defaultProps = {
    type: 'text'
}

export default BaseInput;

// import { InputHTMLAttributes } from "react";
//
// type Props = InputHTMLAttributes<HTMLInputElement>
//
// export default function BaseInput(props: Props)
// {
//     return <input className="govuk-input" {...props} />;
// }
//
// BaseInput.defaultProps = {
//     type: 'text'
// }
